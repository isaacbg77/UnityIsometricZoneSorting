using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace IsometricZoneSorting
{
    /// <summary>
    /// <see cref="IZoneSortable"/> for static objects that sit on a sorting line
    /// (walls, fences, doors, railings). <see cref="SortPosition"/> is derived from the
    /// referenced <see cref="ZoneSortingLine"/> and nudged a hair onto its back side,
    /// so the sortable resolves into the zone just behind the line.
    /// <see cref="SortOrderBias"/> is <c>stride - 1</c>, which lands the sortable
    /// exactly on the zone's front boundary — strictly above the back zone and
    /// strictly below the front zone, so it never ties with movers on either side.
    /// </summary>
    [RequireComponent(typeof(SortingGroup))]
    public class BoundaryZoneSortable : MonoBehaviour, IZoneSortable
    {
        [SerializeField, Tooltip("The sorting line this object sits on. SortPosition is the midpoint of the line's two SortingPoints, offset slightly onto its back side.")]
        private ZoneSortingLine? _line;

        private const float BackSideEpsilon = 0.01f;

        private IZoneSortingService? _zoneSortingService;
        private SortingGroup? _sortingGroup;

        public SortingGroup SortingGroup => _sortingGroup ?? throw new NullReferenceException();

        public Vector2 SortPosition
        {
            get
            {
                if (_line == null) return transform.position;

                var pointA = _line.SortingPointA;
                var pointB = _line.SortingPointB;
                if (pointA == null || pointB == null) return transform.position;

                var midpoint = (pointA.Position + pointB.Position) * 0.5f;
                return midpoint - _line.FrontNormal * BackSideEpsilon;
            }
        }

        public int SortOrderBias => (_zoneSortingService?.ZoneOrderStride ?? 1) - 1;

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
