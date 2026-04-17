using System.Collections.Generic;
using UnityEngine;

namespace YoWorld.Core.Sorting
{
    /// <summary>
    /// Computes depth zones from a set of sorting lines and provides spatial queries.
    /// Each line partitions the scene into a "front" and "back" side.
    /// Zones are regions that share the same ZoneSignature for all lines.
    /// A topological sort assigns each zone a sortingOrderInLayer (0, 1, 2...).
    /// </summary>
    public class ZoneGraph
    {
        private readonly List<ZoneSortingLine> _lines;
        private readonly List<ZoneDefinition> _zones;
        private readonly Dictionary<ZoneSignature, ZoneDefinition> _zonesBySignature;

        public IReadOnlyList<ZoneSortingLine> Lines => _lines;
        public IReadOnlyList<ZoneDefinition> Zones => _zones;

        public ZoneGraph(IReadOnlyList<ZoneSortingLine> lines)
        {
            _lines = new List<ZoneSortingLine>(lines);
            _zones = new List<ZoneDefinition>();
            _zonesBySignature = new Dictionary<ZoneSignature, ZoneDefinition>();
            BuildGraph();
        }

        private void BuildGraph()
        {
            if (_lines.Count == 0)
            {
                var emptySignature = new ZoneSignature(System.Array.Empty<bool>());
                var zone = new ZoneDefinition(0, emptySignature);
                _zones.Add(zone);
                _zonesBySignature[emptySignature] = zone;
                return;
            }

            var signatures = CalculateAllSignatures();
            var adjacency = BuildAdjacencyGraph(signatures);
            var sortedOrders = TopologicalSort(signatures.Count, adjacency);

            for (var zoneIndex = 0; zoneIndex < signatures.Count; zoneIndex++)
            {
                var zone = new ZoneDefinition(sortedOrders[zoneIndex], signatures[zoneIndex]);
                _zones.Add(zone);
                _zonesBySignature[signatures[zoneIndex]] = zone;
            }
        }

        public int GetSortingOrderInLayer(Vector2 worldPosition)
        {
            if (_zones.Count == 0) return 0;
            if (_lines.Count == 0) return _zones[0].SortingOrderInLayer;

            var signature = ComputeSignatureForPosition(worldPosition);

            if (_zonesBySignature.TryGetValue(signature, out var zone))
            {
                return zone.SortingOrderInLayer;
            }

            var closestZone = FindClosestMatchingZone(signature);
            return closestZone.SortingOrderInLayer;
        }

        private List<ZoneSignature> CalculateAllSignatures()
        {
            // With N lines, each region of the scene has a unique combination of
            // front/back sides. Enumerate all 2^N combinations. Zones that are
            // geometrically unreachable simply never get objects assigned to them.
            var signatures = new List<ZoneSignature>();
            var lineCount = _lines.Count;
            var totalCombinations = 1 << lineCount;

            for (var combo = 0; combo < totalCombinations; combo++)
            {
                var sides = new bool[lineCount];
                for (var lineIndex = 0; lineIndex < lineCount; lineIndex++)
                {
                    // Extract bit at lineIndex position: 1 = front side, 0 = back side
                    sides[lineIndex] = (combo & (1 << lineIndex)) != 0;
                }
                signatures.Add(new ZoneSignature(sides));
            }

            return signatures;
        }

        /// <summary>
        /// Tests whether a point is on the front side of a sorting line.
        /// The line is treated as infinite (extending beyond both endpoints)
        /// to ensure clean, continuous zone boundaries without fragmentation.
        /// </summary>
        private static bool IsOnFrontSide(Vector2 point, ZoneSortingLine line)
        {
            var pointA = line.SortingPointA!.Position;
            var pointB = line.SortingPointB!.Position;
            var frontNormal = line.FrontNormal;

            var lineDirection = pointB - pointA;
            var pointVector = point - pointA;

            // Cross product gives signed area; sign indicates which side of the line
            var crossProduct = lineDirection.x * pointVector.y - lineDirection.y * pointVector.x;

            // Determine which side the front normal is on
            var normalCross = lineDirection.x * frontNormal.y - lineDirection.y * frontNormal.x;

            // Point is on the front side if it's on the same side as the front normal
            return (crossProduct >= 0f) == (normalCross >= 0f);
        }

        private ZoneSignature ComputeSignatureForPosition(Vector2 worldPosition)
        {
            var sides = new bool[_lines.Count];

            for (var lineIndex = 0; lineIndex < _lines.Count; lineIndex++)
            {
                sides[lineIndex] = IsOnFrontSide(worldPosition, _lines[lineIndex]);
            }

            return new ZoneSignature(sides);
        }

        private ZoneDefinition FindClosestMatchingZone(ZoneSignature signature)
        {
            var bestZone = _zones[0];
            var bestMatchCount = -1;

            foreach (var zone in _zones)
            {
                var matchCount = signature.CountMatches(zone.Signature);

                if (matchCount > bestMatchCount)
                {
                    bestMatchCount = matchCount;
                    bestZone = zone;
                }
            }

            return bestZone;
        }

        /// <summary>
        /// Builds a directed acyclic graph (DAG) of zone adjacency.
        /// Two zones are adjacent if their signatures differ by exactly one line.
        /// The zone on the front side of that differing line gets an incoming edge
        /// from the zone on the back side, meaning "front zone renders on top of back zone".
        /// </summary>
        private static Dictionary<int, List<int>> BuildAdjacencyGraph(List<ZoneSignature> signatures)
        {
            var adjacency = new Dictionary<int, List<int>>();
            for (var zoneIndex = 0; zoneIndex < signatures.Count; zoneIndex++)
            {
                adjacency[zoneIndex] = new List<int>();
            }

            for (var zoneA = 0; zoneA < signatures.Count; zoneA++)
            {
                for (var zoneB = zoneA + 1; zoneB < signatures.Count; zoneB++)
                {
                    var differingLineIndex = signatures[zoneA].FindSingleDifferingLine(signatures[zoneB]);
                    if (differingLineIndex < 0) continue;

                    // The zone whose signature is true for this line is "in front"
                    if (signatures[zoneA].IsOnFrontSide(differingLineIndex))
                    {
                        // zoneA is in front of zoneB → edge from zoneB to zoneA
                        adjacency[zoneB].Add(zoneA);
                    }
                    else
                    {
                        // zoneB is in front of zoneA → edge from zoneA to zoneB
                        adjacency[zoneA].Add(zoneB);
                    }
                }
            }

            return adjacency;
        }

        /// <summary>
        /// Assigns a sorting order to each zone using Kahn's algorithm for topological sorting.
        /// Zones with no incoming edges (nothing behind them) get order 0.
        /// Each subsequent depth level increments the order by 1.
        /// Detects cycles (contradictory line orientations) and assigns a fallback order.
        /// </summary>
        private static int[] TopologicalSort(int zoneCount, Dictionary<int, List<int>> adjacency)
        {
            var inDegree = new int[zoneCount];
            foreach (var neighbors in adjacency.Values)
            {
                foreach (var neighbor in neighbors)
                {
                    inDegree[neighbor]++;
                }
            }

            var queue = new Queue<int>();
            for (var zoneIndex = 0; zoneIndex < zoneCount; zoneIndex++)
            {
                if (inDegree[zoneIndex] == 0)
                {
                    queue.Enqueue(zoneIndex);
                }
            }

            var sortingOrders = new int[zoneCount];
            for (var i = 0; i < zoneCount; i++) 
            {
                sortingOrders[i] = -1; 
            }

            var currentOrder = 0;
            var processedCount = 0;

            while (queue.Count > 0)
            {
                var batchSize = queue.Count;
                for (var batchIndex = 0; batchIndex < batchSize; batchIndex++)
                {
                    var zoneIndex = queue.Dequeue();
                    sortingOrders[zoneIndex] = currentOrder;
                    processedCount++;

                    if (adjacency.TryGetValue(zoneIndex, out var neighbors))
                    {
                        foreach (var neighbor in neighbors)
                        {
                            inDegree[neighbor]--;
                            if (inDegree[neighbor] == 0)
                            {
                                queue.Enqueue(neighbor);
                            }
                        }
                    }
                }
                currentOrder++;
            }

            if (processedCount < zoneCount)
            {
                Debug.LogWarning("[ZoneGraph]: Cycle detected in zone graph. Some zones may have incorrect sorting orders.");
                for (var zoneIndex = 0; zoneIndex < zoneCount; zoneIndex++)
                {
                    // Now we can safely check for -1
                    if (sortingOrders[zoneIndex] == -1)
                    {
                        sortingOrders[zoneIndex] = currentOrder;
                    }
                }
            }

            return sortingOrders;
        }
    }
}
