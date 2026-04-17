using UnityEngine;
using UnityEngine.Rendering;

namespace IsometricZoneSorting
{
    public interface IZoneSortable
    {
        public SortingGroup SortingGroup { get; }
        public Vector2 SortPosition { get; }

        /// <summary>
        /// Integer added to the zone's sorting order when this sortable is rendered.
        /// Lets sortables (e.g. walls, fences) sit in an intermediate slot between
        /// adjacent zones so they never tie with movers. Must be in
        /// <c>[0, ZoneGraph.ZoneOrderStride)</c>.
        /// </summary>
        public int SortOrderBias => 0;
    }
}
