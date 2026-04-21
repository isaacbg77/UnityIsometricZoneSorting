using System.Collections.Generic;
using UnityEngine;

namespace IsometricZoneSorting
{
    /// <summary>
    /// Computes depth zones from a set of sorting lines and provides spatial queries.
    /// Each line partitions the scene into a "front" and "back" side for signature
    /// computation, but only separates zones along its finite segment: regions whose
    /// only separator is the segment's (infinite) extension past an endpoint collapse
    /// into a single merged zone.
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
        private const float GeometryEpsilon = 1e-4f;

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

        public ZoneGraph(IReadOnlyList<ZoneSortingLine> lines, int zoneOrderStride = 10)
        {
            if (zoneOrderStride < 1) throw new System.ArgumentOutOfRangeException(nameof(zoneOrderStride), "Stride must be at least 1.");

            _lines = new List<ZoneSortingLine>(lines);
            _zones = new List<ZoneDefinition>();
            _zonesBySignature = new Dictionary<ZoneSignature, ZoneDefinition>();
            _zoneOrderStride = zoneOrderStride;

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

            var worldPolygon = BuildWorldPolygon();
            var signatures = new List<ZoneSignature>();
            var polygons = new List<List<Vector2>>();
            EnumerateNonEmptySignatures(worldPolygon, signatures, polygons);

            var sigToIndex = new Dictionary<ZoneSignature, int>(signatures.Count);
            for (var i = 0; i < signatures.Count; i++)
            {
                sigToIndex[signatures[i]] = i;
            }

            var parent = new int[signatures.Count];
            for (var i = 0; i < parent.Length; i++) parent[i] = i;

            // (backIndex, frontIndex) — only when a real segment actually separates the pair.
            var separatingEdges = new List<(int back, int front)>();

            for (var i = 0; i < signatures.Count; i++)
            {
                for (var k = 0; k < _lines.Count; k++)
                {
                    var neighborSig = FlipBit(signatures[i], k);
                    if (!sigToIndex.TryGetValue(neighborSig, out var j)) continue;
                    if (j <= i) continue;

                    if (SharedBoundaryOverlapsSegment(polygons[i], _lines[k]))
                    {
                        if (signatures[i].IsOnFrontSide(k))
                            separatingEdges.Add((j, i));
                        else
                            separatingEdges.Add((i, j));
                    }
                    else
                    {
                        Union(parent, i, j);
                    }
                }
            }

            var rootToCluster = new Dictionary<int, int>();
            var sigToCluster = new int[signatures.Count];
            for (var i = 0; i < signatures.Count; i++)
            {
                var root = Find(parent, i);
                if (!rootToCluster.TryGetValue(root, out var clusterId))
                {
                    clusterId = rootToCluster.Count;
                    rootToCluster[root] = clusterId;
                }
                sigToCluster[i] = clusterId;
            }

            var clusterCount = rootToCluster.Count;
            var representativeSig = new ZoneSignature[clusterCount];
            for (var i = 0; i < signatures.Count; i++)
            {
                var c = sigToCluster[i];
                if (representativeSig[c] is null) representativeSig[c] = signatures[i];
            }

            var clusterAdjacency = new Dictionary<int, List<int>>(clusterCount);
            for (var c = 0; c < clusterCount; c++) clusterAdjacency[c] = new List<int>();
            var seenClusterEdges = new HashSet<long>();
            foreach (var (back, front) in separatingEdges)
            {
                var cBack = sigToCluster[back];
                var cFront = sigToCluster[front];
                if (cBack == cFront) continue;
                var key = ((long)cBack << 32) | (uint)cFront;
                if (seenClusterEdges.Add(key))
                {
                    clusterAdjacency[cBack].Add(cFront);
                }
            }

            var sortedOrders = TopologicalSort(clusterCount, clusterAdjacency, _zoneOrderStride);

            for (var c = 0; c < clusterCount; c++)
            {
                _zones.Add(new ZoneDefinition(sortedOrders[c], representativeSig[c]));
            }
            for (var i = 0; i < signatures.Count; i++)
            {
                _zonesBySignature[signatures[i]] = _zones[sigToCluster[i]];
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
        /// The line is treated as infinite here; whether the segment's extension past
        /// its endpoints actually *separates* zones is decided later, in BuildGraph,
        /// by the per-pair shared-boundary test.
        /// </summary>
        /// <param name="point">The point to test.</param>
        /// <param name="line">The sorting line to test against.</param>
        /// <returns>True if the point is on the front side of the line, false otherwise.</returns>
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
        /// Builds a convex rectangle (CCW) large enough to contain every sorting line
        /// plus a generous margin. Used as the clipping frame for per-signature zone
        /// polygons; its size does not affect sort-order correctness as long as all
        /// queried world positions fall inside it.
        /// </summary>
        private List<Vector2> BuildWorldPolygon()
        {
            var minX = float.PositiveInfinity;
            var minY = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var maxY = float.NegativeInfinity;

            foreach (var line in _lines)
            {
                var a = line.SortingPointA!.Position;
                var b = line.SortingPointB!.Position;
                if (a.x < minX) minX = a.x;
                if (b.x < minX) minX = b.x;
                if (a.y < minY) minY = a.y;
                if (b.y < minY) minY = b.y;
                if (a.x > maxX) maxX = a.x;
                if (b.x > maxX) maxX = b.x;
                if (a.y > maxY) maxY = a.y;
                if (b.y > maxY) maxY = b.y;
            }

            var width = Mathf.Max(maxX - minX, 1f);
            var height = Mathf.Max(maxY - minY, 1f);
            var margin = 10f * Mathf.Max(width, height);

            minX -= margin;
            minY -= margin;
            maxX += margin;
            maxY += margin;

            return new List<Vector2>
            {
                new(minX, minY),
                new(maxX, minY),
                new(maxX, maxY),
                new(minX, maxY),
            };
        }

        /// <summary>
        /// For each of the 2^N raw signatures, clips the world polygon by every line's
        /// half-plane in the signature. Keeps only the signatures whose resulting
        /// polygon is non-degenerate.
        /// </summary>
        private void EnumerateNonEmptySignatures(
            List<Vector2> worldPolygon,
            List<ZoneSignature> signatures,
            List<List<Vector2>> polygons)
        {
            var lineCount = _lines.Count;
            var totalCombinations = 1 << lineCount;

            for (var combo = 0; combo < totalCombinations; combo++)
            {
                var polygon = new List<Vector2>(worldPolygon);
                var sides = new bool[lineCount];
                var isEmpty = false;

                for (var lineIndex = 0; lineIndex < lineCount; lineIndex++)
                {
                    var front = (combo & (1 << lineIndex)) != 0;
                    sides[lineIndex] = front;
                    polygon = ClipPolygonByHalfPlane(polygon, _lines[lineIndex], front);
                    if (polygon.Count < 3)
                    {
                        isEmpty = true;
                        break;
                    }
                }

                if (isEmpty) continue;

                signatures.Add(new ZoneSignature(sides));
                polygons.Add(polygon);
            }
        }

        /// <summary>
        /// Sutherland–Hodgman clip: keeps the part of <paramref name="input"/> that lies
        /// on the <paramref name="front"/>-or-back side of <paramref name="line"/>.
        /// </summary>
        private static List<Vector2> ClipPolygonByHalfPlane(List<Vector2> input, ZoneSortingLine line, bool front)
        {
            if (input.Count == 0) return input;

            var a = line.SortingPointA!.Position;
            var b = line.SortingPointB!.Position;
            var dir = b - a;

            // Left-hand perpendicular to AB. Sign flipped to agree with IsOnFrontSide.
            var leftPerp = new Vector2(-dir.y, dir.x);
            var normalCross = dir.x * line.FrontNormal.y - dir.y * line.FrontNormal.x;
            var halfPlaneNormal = normalCross >= 0f ? leftPerp : -leftPerp;
            if (!front) halfPlaneNormal = -halfPlaneNormal;

            var output = new List<Vector2>(input.Count + 2);

            for (var i = 0; i < input.Count; i++)
            {
                var curr = input[i];
                var prev = input[(i - 1 + input.Count) % input.Count];
                var currSide = Vector2.Dot(curr - a, halfPlaneNormal);
                var prevSide = Vector2.Dot(prev - a, halfPlaneNormal);
                var currInside = currSide >= 0f;
                var prevInside = prevSide >= 0f;

                if (currInside)
                {
                    if (!prevInside)
                    {
                        output.Add(IntersectEdgeWithLine(prev, curr, a, halfPlaneNormal));
                    }
                    output.Add(curr);
                }
                else if (prevInside)
                {
                    output.Add(IntersectEdgeWithLine(prev, curr, a, halfPlaneNormal));
                }
            }

            return output;
        }

        private static Vector2 IntersectEdgeWithLine(Vector2 p0, Vector2 p1, Vector2 linePoint, Vector2 normal)
        {
            var edge = p1 - p0;
            var denom = Vector2.Dot(edge, normal);
            if (Mathf.Abs(denom) < Mathf.Epsilon) return p0;
            var t = Vector2.Dot(linePoint - p0, normal) / denom;
            return p0 + t * edge;
        }

        /// <summary>
        /// Returns whether the edge of <paramref name="polygon"/> that lies on
        /// <paramref name="line"/>'s supporting line overlaps the finite segment
        /// between the line's two endpoints. Convex polygons share at most one such
        /// edge with a supporting line.
        /// </summary>
        private static bool SharedBoundaryOverlapsSegment(List<Vector2> polygon, ZoneSortingLine line)
        {
            var a = line.SortingPointA!.Position;
            var b = line.SortingPointB!.Position;
            var dir = b - a;
            var dirLenSq = Vector2.Dot(dir, dir);
            if (dirLenSq <= Mathf.Epsilon) return false;

            var lineNormal = new Vector2(-dir.y, dir.x); // length == |dir|
            var normalThreshold = GeometryEpsilon * Mathf.Sqrt(dirLenSq);

            var tLo = float.PositiveInfinity;
            var tHi = float.NegativeInfinity;

            for (var i = 0; i < polygon.Count; i++)
            {
                var p0 = polygon[i];
                var p1 = polygon[(i + 1) % polygon.Count];
                if (Mathf.Abs(Vector2.Dot(p0 - a, lineNormal)) > normalThreshold) continue;
                if (Mathf.Abs(Vector2.Dot(p1 - a, lineNormal)) > normalThreshold) continue;

                var t0 = Vector2.Dot(p0 - a, dir) / dirLenSq;
                var t1 = Vector2.Dot(p1 - a, dir) / dirLenSq;
                if (t0 > t1) (t0, t1) = (t1, t0);
                if (t0 < tLo) tLo = t0;
                if (t1 > tHi) tHi = t1;
            }

            if (tLo == float.PositiveInfinity) return false;

            var overlapLo = Mathf.Max(tLo, 0f);
            var overlapHi = Mathf.Min(tHi, 1f);
            return overlapHi - overlapLo > GeometryEpsilon;
        }

        private static ZoneSignature FlipBit(ZoneSignature signature, int bitIndex)
        {
            var length = signature.LineCount;
            var sides = new bool[length];
            for (var i = 0; i < length; i++) sides[i] = signature.IsOnFrontSide(i);
            sides[bitIndex] = !sides[bitIndex];
            return new ZoneSignature(sides);
        }

        private static int Find(int[] parent, int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }
            return x;
        }

        private static void Union(int[] parent, int a, int b)
        {
            var ra = Find(parent, a);
            var rb = Find(parent, b);
            if (ra == rb) return;
            parent[ra] = rb;
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
