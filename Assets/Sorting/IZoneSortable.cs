using UnityEngine;
using UnityEngine.Rendering;

namespace YoWorld.Core.Sorting
{
    public interface IZoneSortable
    {
        public SortingGroup SortingGroup { get; }
        public Vector2 SortPosition { get; }
    }
}
