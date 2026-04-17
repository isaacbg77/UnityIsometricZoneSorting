using UnityEngine;

namespace IsometricZoneSorting
{
    /// <summary>
    /// ScriptableObject wrapper around a sorting layer name. Exposes the resolved
    /// sorting layer ID so consumers can assign it to a SortingGroup at runtime.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SortingLayerReference",
        menuName = "Isometric Zone Sorting/Sorting Layer Reference")]
    public class SortingLayerReference : ScriptableObject
    {
        [SerializeField] private string _sortingLayerName = "Default";

        public string Name => _sortingLayerName;
        public int Id => SortingLayer.NameToID(_sortingLayerName);
    }
}
