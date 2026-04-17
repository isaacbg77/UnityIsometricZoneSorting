using UnityEngine;

namespace IsometricZoneSorting
{
    public class ZoneSortingLine : MonoBehaviour
    {
        [Header("Sorting Points")]
        [SerializeField] private SortingPoint? _sortingPointA;
        [SerializeField] private SortingPoint? _sortingPointB;

        [Header("Front Direction")]
        [SerializeField] private Vector2 _frontNormal = Vector2.up;

        public SortingPoint? SortingPointA => _sortingPointA;
        public SortingPoint? SortingPointB => _sortingPointB;
        public Vector2 FrontNormal => _frontNormal.normalized;

        public bool IsValid => _sortingPointA != null && _sortingPointB != null;
    }
}
