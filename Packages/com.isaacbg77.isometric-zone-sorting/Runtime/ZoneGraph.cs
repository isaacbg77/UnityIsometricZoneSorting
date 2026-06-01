using System.Collections.Generic;
using UnityEngine;

namespace IsometricZoneSorting
{
    /// <summary>
    /// Computes depth zones from a set of sorting lines and provides spatial queries.
    /// Each line partitions the scene into a "front" and "back" side.
    /// Zones are regions that share the same ZoneSignature for all lines.
    /// A topological sort assigns each zone a sortingOrderInLayer of
    /// <c>depth · ZoneOrderStride + 1</c>, so that stride multiples
    /// (<c>0, stride, 2·stride, …</c>) are reserved for zone boundaries and each
    /// zone spans the range between two adjacent boundaries. Sortables use
    /// <c>IZoneSortable.SortOrderBias</c> in <c>[0, ZoneOrderStride - 1)</c> to pick
    /// a slot within their zone; a bias of <c>ZoneOrderStride - 1</c> lands exactly
    /// on the front boundary (used by <see cref="BoundaryZoneSortable"/>).
    /// </summary>
    public class ZoneGraph
    {
        private readonly List<ZoneSortingLine> _lines;
        private readonly List<ZoneDefinition> _zones;
        private readonly Dictionary<ZoneSignature, ZoneDefinition> _zonesBySignature;
        private readonly int _zoneOrderStride;

        public IReadOnlyList<ZoneDefinition> Zones => _zones;

        /// <summary>
        /// Distance between adjacent zone boundaries. Boundaries live at
        /// <c>0, stride, 2·stride, …</c>; each zone's first sorting layer is one
        /// above its back boundary (<c>depth · stride + 1</c>).
        /// </summary>
        public int ZoneOrderStride => _zoneOrderStride;

        public ZoneGraph(IReadOnlyList<ZoneSortingLine> lines, IEnumerable<IZoneSortable> allSortables, int zoneOrderStride = 10)
        {
            if (zoneOrderStride < 1) throw new System.ArgumentOutOfRangeException(nameof(zoneOrderStride), "Stride must be at least 1.");

            _lines = new List<ZoneSortingLine>(lines);
            _zones = new List<ZoneDefinition>();
            _zonesBySignature = new Dictionary<ZoneSignature, ZoneDefinition>();
            _zoneOrderStride = zoneOrderStride;

            BuildGraph(allSortables);
        }

        private void BuildGraph(IEnumerable<IZoneSortable> allSortables)
        {
            if (_lines.Count == 0)
            {
                var emptySignature = new ZoneSignature(System.Array.Empty<bool>());
                var zone = new ZoneDefinition(0, emptySignature);

                _zones.Add(zone);
                _zonesBySignature[emptySignature] = zone;
                return;
            }

            var signatures = DiscoverValidSignatures(allSortables);
            var adjacency = BuildAdjacencyGraph(signatures, _lines);
            var sortedOrders = TopologicalSort(signatures.Count, adjacency, _zoneOrderStride);

            for (var zoneIndex = 0; zoneIndex < signatures.Count; zoneIndex++)
            {
                var zone = new ZoneDefinition(sortedOrders[zoneIndex], signatures[zoneIndex]);
                _zones.Add(zone);
                _zonesBySignature[signatures[zoneIndex]] = zone;
            }
        }

        /// <summary>Returns the sorting order of the zone that contains the given world position.</summary>
        /// <param name="worldPosition">The world position to check.</param>
        /// <returns>The sorting order of the zone containing the position, or 0 if no zones are defined.</returns>
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

        /// <summary>Discovers valid signatures based on scene sortables and line offsets.</summary>
        /// <param name="sortables">The collection of all sortables in the scene.</param>
        /// <returns>A list of unique ZoneSignature objects that actually exist in the scene.</returns>
        private List<ZoneSignature> DiscoverValidSignatures(IEnumerable<IZoneSortable> sortables)
        {
            var uniqueSignatures = new HashSet<ZoneSignature>();

            // Sample from all active objects
            foreach (var sortable in sortables)
            {
                uniqueSignatures.Add(ComputeSignatureForPosition(sortable.SortPosition));
            }

            // Sample offsets from every line to ensure zone coverage
            const float offset = 0.1f;
            foreach (var line in _lines)
            {
                if (!line.IsValid) continue;

                var midpoint = (line.SortingPointA!.Position + line.SortingPointB!.Position) * 0.5f;
                uniqueSignatures.Add(ComputeSignatureForPosition(midpoint + line.FrontNormal * offset));
                uniqueSignatures.Add(ComputeSignatureForPosition(midpoint - line.FrontNormal * offset));
            }

            return new List<ZoneSignature>(uniqueSignatures);
        }

        /// <summary>Computes the signature for a given world position.</summary>
        /// <param name="worldPosition">The world position to compute the signature for.</param>
        /// <returns>A ZoneSignature object representing the zone that contains the position.</returns>
        private ZoneSignature ComputeSignatureForPosition(Vector2 worldPosition)
        {
            var sides = new bool[_lines.Count];

            for (var lineIndex = 0; lineIndex < _lines.Count; lineIndex++)
            {
                sides[lineIndex] = IsOnFrontSide(worldPosition, _lines[lineIndex]);
            }

            return new ZoneSignature(sides);
        }

        /// <summary>
        /// Tests whether a point is on the front side of a sorting line.
        /// The line is treated as infinite (extending beyond both endpoints)
        /// to ensure clean, continuous zone boundaries without fragmentation.
        /// Falls back to <c>false</c> (back side) if the line's sorting points
        /// have been destroyed or cleared since the graph was built; lines are
        /// validated at construction time, so this only guards against runtime
        /// teardown.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <param name="line">The sorting line to test against.</param>
        /// <returns>True if the point is on the front side of the line, false otherwise.</returns>
        private static bool IsOnFrontSide(Vector2 point, ZoneSortingLine line)
        {
            var sortingPointA = line.SortingPointA;
            var sortingPointB = line.SortingPointB;
            if (sortingPointA == null || sortingPointB == null) return false;

            var pointA = sortingPointA.Position;
            var pointB = sortingPointB.Position;
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

        /// <summary>Finds the zone with the most matching lines to the given signature.</summary>
        /// <param name="signature">The signature to match against.</param>
        /// <returns>The zone with the most matching lines to the signature.</returns>
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
        /// Builds a directed acyclic graph (DAG) of zone adjacency using geometric slope rules and front normals.
        /// Two zones are adjacent if their signatures differ by exactly one line.
        /// Rendering priority is primarily determined by the <see cref="ZoneSortingLine.FrontNormal"/>
        /// configured for the separating line. In a typical isometric setup (+X -Y is depth):
        /// - Vertical lines: Front side (determined by normal) renders on top.
        /// - Positive slope (ascends right): Typically, Right/Down is in front of Left/Up.
        /// - Negative slope (descends right): Typically, Right/Up is in front of Left/Down.
        /// </summary>
        /// <param name="signatures">A list of ZoneSignature objects representing discovered zone signatures.</param>
        /// <param name="lines">The list of sorting lines.</param>
        /// <returns>A dictionary mapping zone indices to lists of incoming zone indices.</returns>
        private static Dictionary<int, List<int>> BuildAdjacencyGraph(List<ZoneSignature> signatures, List<ZoneSortingLine> lines)
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
                    var adjacentLineIdx = signatures[zoneA].FindAdjacentLineIndex(signatures[zoneB]);
                    if (adjacentLineIdx < 0) continue;

                    var line = lines[adjacentLineIdx];
                    var delta = line.SortingPointB!.Position - line.SortingPointA!.Position;

                    bool aIsFront = signatures[zoneA].IsOnFrontSide(adjacentLineIdx);

                    bool isAInFront;
                    if (Mathf.Abs(delta.x) < 0.001f) // Vertical Line
                    {
                        // Simple front/back logic
                        isAInFront = aIsFront;
                    }
                    else
                    {
                        float slope = delta.y / delta.x;
                        if (slope > 0) // Ascends towards the right
                        {
                            // In standard isometric depth, a positive slope line separates "Front-Right" from "Back-Left".
                            // The FrontNormal should be pointed towards the camera (typically Down/Right).
                            isAInFront = aIsFront;
                        }
                        else // Descends towards the right
                        {
                            // In standard isometric depth, a negative slope line separates "Front-Left" from "Back-Right".
                            // The FrontNormal should be pointed towards the camera (typically Down/Left).
                            isAInFront = aIsFront;
                        }
                    }

                    if (isAInFront)
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
        /// Zones at depth D get order <c>D · stride + 1</c>, leaving the stride multiples
        /// (<c>0, stride, 2·stride, …</c>) free as boundary-only orders.
        /// Detects cycles (contradictory line orientations) and assigns a fallback order.
        /// </summary>
        /// <param name="zoneCount">The number of zones in the graph.</param>
        /// <param name="adjacency">A dictionary mapping zone indices to lists of incoming zone indices.</param>
        /// <returns>An array of zone sorting orders, one for each zone.</returns>
        private static int[] TopologicalSort(int zoneCount, Dictionary<int, List<int>> adjacency, int stride)
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
                    sortingOrders[zoneIndex] = currentOrder * stride + 1;
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
                        sortingOrders[zoneIndex] = currentOrder * stride + 1;
                    }
                }
            }

            return sortingOrders;
        }
    }
}
