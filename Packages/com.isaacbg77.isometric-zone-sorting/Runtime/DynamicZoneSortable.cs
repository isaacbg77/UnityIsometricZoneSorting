using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace IsometricZoneSorting
{
    /// <summary>
    /// Default <see cref="IZoneSortable"/> for things that move (characters, props, items).
    /// <see cref="SortPosition"/> tracks <c>transform.position</c> each frame.
    /// For static objects sitting on a sorting line (walls, fences, doors) use
    /// <see cref="BoundaryZoneSortable"/> instead.
    /// </summary>
    [RequireComponent(typeof(SortingGroup))]
    public class DynamicZoneSortable : MonoBehaviour, IZoneSortable
    {
        private IZoneSortingService? _zoneSortingService;
        private SortingGroup? _sortingGroup;

        public SortingGroup SortingGroup => _sortingGroup ?? throw new NullReferenceException();
        public Vector2 SortPosition => transform.position;

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
