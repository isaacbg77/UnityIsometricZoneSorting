using UnityEngine;
using UnityEngine.Rendering;

namespace IsometricZoneSorting
{
    public interface IZoneSortable
    {
        public SortingGroup SortingGroup { get; }
        public Vector2 SortPosition { get; }
    }
}
