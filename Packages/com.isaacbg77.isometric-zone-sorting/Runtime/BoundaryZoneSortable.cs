using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace IsometricZoneSorting
{
    /// <summary>
    /// <see cref="IZoneSortable"/> for static objects that sit on a sorting line
    /// (walls, fences, doors, railings). <see cref="SortPosition"/> is derived from the
    /// referenced <see cref="ZoneSortingLine"/> and nudged a hair onto its back side,
    /// so the sortable resolves into the zone just behind the line. Combined with a
    /// positive <see cref="SortOrderBias"/> (defaults to 1), it ends up in the gap
    /// between the line's back and front zones — no tie with movers on either side.
    /// </summary>
    [RequireComponent(typeof(SortingGroup))]
    public class BoundaryZoneSortable : MonoBehaviour, IZoneSortable
    {
        [SerializeField, Tooltip("The sorting line this object sits on. SortPosition is the midpoint of the line's two SortingPoints, offset slightly onto its back side.")]
        private ZoneSortingLine? _line;

        [SerializeField, Min(0), Tooltip("Offset added to the zone's sorting order. Must be in [0, stride). Default 1 puts this sortable strictly above the back zone and strictly below the front zone.")]
        private int _sortOrderBias = 1;

        // Small nudge along -FrontNormal so the query point is unambiguously on the
        // back side of the line in ZoneGraph's cross-product test.
        private const float BackSideEpsilon = 0.01f;

        private IZoneSortingService? _zoneSortingService;
        private SortingGroup? _sortingGroup;

        public SortingGroup SortingGroup => _sortingGroup ?? throw new NullReferenceException();

        public Vector2 SortPosition
        {
            get
            {
                if (_line == null || !_line.IsValid) return transform.position;
                var midpoint = (_line.SortingPointA!.Position + _line.SortingPointB!.Position) * 0.5f;
                return midpoint - _line.FrontNormal * BackSideEpsilon;
            }
        }

        public int SortOrderBias => _sortOrderBias;

        private void Awake()
        {
            _zoneSortingService = SceneUtils.FindInterfaceOfType<IZoneSortingService>();
            if (_zoneSortingService == null) Debug.LogError($"[{nameof(BoundaryZoneSortable)}]: {nameof(IZoneSortingService)} is null", this);

            _sortingGroup = GetComponent<SortingGroup>();
            if (_sortingGroup == null) Debug.LogError($"[{nameof(BoundaryZoneSortable)}]: {nameof(_sortingGroup)} is null", this);

            if (_line == null) Debug.LogError($"[{nameof(BoundaryZoneSortable)}]: {nameof(_line)} is not assigned", this);
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
