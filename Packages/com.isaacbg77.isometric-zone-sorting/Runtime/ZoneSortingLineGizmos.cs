using UnityEngine;

namespace IsometricZoneSorting
{
    [RequireComponent(typeof(ZoneSortingLine))]
    public class ZoneSortingLineGizmos : MonoBehaviour
    {
        private ZoneSortingLine? _sortingLine;

        private void Awake()
        {
            GetDependencies();
        }
        
        private void OnValidate()
        {
            GetDependencies();
        }

        private void GetDependencies()
        {
            _sortingLine = GetComponent<ZoneSortingLine>();
        }

        private void OnDrawGizmos()
        {
            if (_sortingLine == null) return;
            if (_sortingLine.SortingPointA == null || _sortingLine.SortingPointB == null) return;

            var pointA = (Vector3)_sortingLine.SortingPointA.Position;
            var pointB = (Vector3)_sortingLine.SortingPointB.Position;
            var midpoint = (pointA + pointB) * 0.5f;
            var normal = (Vector3)_sortingLine.FrontNormal;

            // Draw the sorting line
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pointA, pointB);

            // Draw endpoints
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(pointA, 0.1f);
            Gizmos.DrawSphere(pointB, 0.1f);

            // Draw front direction arrow
            Gizmos.color = Color.cyan;
            var arrowEnd = midpoint + normal * 0.5f;
            Gizmos.DrawLine(midpoint, arrowEnd);
            Gizmos.DrawSphere(arrowEnd, 0.05f);
        }
    }
}
