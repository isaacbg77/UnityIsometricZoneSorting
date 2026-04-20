using UnityEngine;
using UnityEngine.Rendering;

namespace IsometricZoneSorting
{
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
