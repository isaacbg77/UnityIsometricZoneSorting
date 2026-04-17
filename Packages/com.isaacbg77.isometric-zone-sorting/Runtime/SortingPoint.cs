using UnityEngine;

namespace IsometricZoneSorting
{
    public class SortingPoint : MonoBehaviour
    {
        [SerializeField] private float _maxVerticalDistance = 5f;
        [SerializeField] private float _maxHorizontalDistance = 5f;

        public Vector2 Position => transform.position;
        
        public float MaxVerticalDistance => _maxVerticalDistance;
        public float MaxHorizontalDistance => _maxHorizontalDistance;
    }
}
