using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IsometricZoneSorting
{
    [RequireComponent(typeof(ZoneSortingService))]
    public class ZoneSortingGizmos : MonoBehaviour
    {
        private static readonly Color[] ZoneColors =
        {
            new(0.2f, 0.4f, 1f, 0.4f),
            new(1f, 0.4f, 0.2f, 0.4f),
            new(0.2f, 1f, 0.4f, 0.4f),
            new(1f, 1f, 0.2f, 0.4f),
            new(0.8f, 0.2f, 1f, 0.4f),
            new(0.2f, 1f, 1f, 0.4f),
            new(1f, 0.6f, 0.8f, 0.4f),
            new(0.6f, 0.8f, 0.4f, 0.4f),
        };

        [SerializeField] private Vector2 _gridOrigin = new(-20f, -20f);
        [SerializeField] private Vector2 _gridSize = new(40f, 40f);
        [SerializeField] private float _cellSize = 0.5f;

        private void OnDrawGizmosSelected()
        {
            var sortingLines = FindObjectsByType<ZoneSortingLine>(FindObjectsSortMode.None);
            if (sortingLines.Length == 0) return;

            var validLines = sortingLines
                .Where(line => line.IsValid)
                .ToList();
            if (validLines.Count == 0) return;

            var graph = new ZoneGraph(validLines);

            // Draw zone overlays by sampling a grid
            var columnsCount = Mathf.CeilToInt(_gridSize.x / _cellSize);
            var rowsCount = Mathf.CeilToInt(_gridSize.y / _cellSize);

            for (var column = 0; column < columnsCount; column++)
            {
                for (var row = 0; row < rowsCount; row++)
                {
                    var worldPosition = _gridOrigin + new Vector2(column * _cellSize + _cellSize * 0.5f, row * _cellSize + _cellSize * 0.5f);
                    var sortingOrder = graph.GetSortingOrderInLayer(worldPosition);
                    var colorIndex = sortingOrder % ZoneColors.Length;

                    Gizmos.color = ZoneColors[colorIndex];
                    Gizmos.DrawCube(worldPosition, new Vector3(_cellSize, _cellSize, 0f));
                }
            }

            // Draw order labels at the center of each unique sorting order region
            var labelledOrders = new HashSet<int>();
            foreach (var zone in graph.Zones)
            {
                if (labelledOrders.Contains(zone.SortingOrderInLayer)) continue;
                if (!TryFindOrderCenter(zone.SortingOrderInLayer, graph, out var center)) continue;
                labelledOrders.Add(zone.SortingOrderInLayer);
#if UNITY_EDITOR
                var labelStyle = new GUIStyle
                {
                    normal = { textColor = Color.white },
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                UnityEditor.Handles.Label(center, $"Order: {zone.SortingOrderInLayer}", labelStyle);
#endif
            }
        }

        private bool TryFindOrderCenter(int sortingOrder, ZoneGraph graph, out Vector3 center)
        {
            var sum = Vector2.zero;
            var count = 0;

            var columnsCount = Mathf.CeilToInt(_gridSize.x / _cellSize);
            var rowsCount = Mathf.CeilToInt(_gridSize.y / _cellSize);
            var sampleStep = Mathf.Max(1, Mathf.CeilToInt(columnsCount / 20f));

            for (var column = 0; column < columnsCount; column += sampleStep)
            {
                for (var row = 0; row < rowsCount; row += sampleStep)
                {
                    var worldPosition = _gridOrigin + new Vector2(column * _cellSize + _cellSize * 0.5f, row * _cellSize + _cellSize * 0.5f);
                    if (graph.GetSortingOrderInLayer(worldPosition) == sortingOrder)
                    {
                        sum += worldPosition;
                        count++;
                    }
                }
            }

            if (count == 0)
            {
                center = Vector3.zero;
                return false;
            }

            center = sum / count;
            return true;
        }
    }
}
