using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace IsometricZoneSorting
{
    [RequireComponent(typeof(SortingGroup))]
    public class ZoneSortable : MonoBehaviour, IZoneSortable
    {
        [SerializeField, Tooltip("Offset added to the zone's sorting order. Use a positive value (e.g. 1) for sortables sitting on a zone boundary, like walls, so they never tie with movers. Must be in [0, ZoneGraph.ZoneOrderStride).")]
        private int _sortOrderBias;

        private IZoneSortingService? _zoneSortingService;
        private SortingGroup? _sortingGroup;

        public SortingGroup SortingGroup => _sortingGroup ?? throw new NullReferenceException();
        public Vector2 SortPosition => transform.position;
        public int SortOrderBias => _sortOrderBias;

        private void Awake()
        {
            _zoneSortingService = SceneUtils.FindInterfaceOfType<IZoneSortingService>();
            if (_zoneSortingService == null) Debug.LogError($"[{nameof(ZoneSortable)}]: {nameof(IZoneSortingService)} is null", this);

            _sortingGroup = GetComponent<SortingGroup>();
            if (_sortingGroup == null) Debug.LogError($"[{nameof(ZoneSortable)}]: {nameof(_sortingGroup)} is null", this);
        }

        private void OnEnable()
        {
            if (_zoneSortingService == null || _sortingGroup == null) return;
            _zoneSortingService.Register(this);
        }

        private void OnDisable()
        {
            _zoneSortingService?.Unregister(this);
        }
    }
}
