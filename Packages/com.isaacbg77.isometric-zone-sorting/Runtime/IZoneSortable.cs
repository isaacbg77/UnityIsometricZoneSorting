using UnityEngine;
using UnityEngine.Rendering;

namespace IsometricZoneSorting
{
    /// <summary>
    /// Base contract for anything whose depth should be driven by the zone graph.
    /// Consumers don't implement this directly — they implement
    /// <see cref="IDynamicZoneSortable"/> (re-resolved every frame) or
    /// <see cref="IStaticZoneSortable"/> (resolved once per graph build), both of
    /// which extend this interface.
    /// </summary>
    public interface IZoneSortable
    {
        public SortingGroup SortingGroup { get; }
        public Vector2 SortPosition { get; }

        /// <summary>
        /// Offset added to the first sorting layer of this sortable's zone to pick
        /// a slot within that zone. Must be in <c>[0, stride - 1)</c> for sortables
        /// that should stay inside the zone, where stride is
        /// <see cref="IZoneSortingService.ZoneOrderStride"/>. A value of
        /// <c>stride - 1</c> lands on the zone's front boundary (used by
        /// <see cref="BoundaryZoneSortable"/> for things sitting on a sorting line).
        /// </summary>
        public int SortOrderBias => 0;
    }
}
