using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace IsometricZoneSorting
{
    /// <summary>
    /// Default <see cref="IZoneSortable"/> for things that move (characters, props, items).
    /// <see cref="SortPosition"/> tracks <c>transform.position</c> each frame.
    /// <see cref="SortOrderBias"/> defaults to <c>0</c> (the first sorting layer in the
    /// sortable's zone) and can be raised to stack above other movers in the same zone.
    /// For static objects sitting on a sorting line (walls, fences, doors) use
    /// <see cref="BoundaryZoneSortable"/> instead.
    /// </summary>
    [RequireComponent(typeof(SortingGroup))]
    public class DynamicZoneSortable : MonoBehaviour, IZoneSortable
    {
        [SerializeField, Min(0), Tooltip("Offset added to the zone's first sorting layer. 0 (default) puts this sortable on the first layer in its zone; raise it to stack above other movers within the same zone. Must be less than stride-1 to stay inside the zone; stride-1 lands on the zone's front boundary.")]
        private int _sortOrderBias;

        private IZoneSortingService? _zoneSortingService;
        private SortingGroup? _sortingGroup;

        public SortingGroup SortingGroup => _sortingGroup ?? throw new NullReferenceException();
        public Vector2 SortPosition => transform.position;
        public int SortOrderBias => _sortOrderBias;

        private void Awake()
        {
            _zoneSortingService = SceneUtils.FindInterfaceOfType<IZoneSortingService>();
            if (_zoneSortingService == null) Debug.LogError($"[{nameof(DynamicZoneSortable)}]: {nameof(IZoneSortingService)} is null", this);

            _sortingGroup = GetComponent<SortingGroup>();
            if (_sortingGroup == null) Debug.LogError($"[{nameof(DynamicZoneSortable)}]: {nameof(_sortingGroup)} is null", this);
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
