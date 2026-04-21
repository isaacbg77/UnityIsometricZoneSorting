using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IsometricZoneSorting
{
    public class ZoneSortingService : MonoBehaviour, IZoneSortingService
    {
        [SerializeField, SortingLayer] private string _zoneSortingLayer = "Default";

        [SerializeField, Min(2), Tooltip("Distance between adjacent zone boundaries. Boundary orders are 0, stride, 2·stride, …; zones occupy the integers in between. A sortable's SortOrderBias (0 to stride-2) picks a slot inside its zone; BoundaryZoneSortable uses stride-1 to land on the boundary itself.")]
        private int _zoneOrderStride = 10;

        [SerializeField] private bool _rebuildZonesOnAwake = true;

        private readonly HashSet<IDynamicZoneSortable> _dynamicSortables = new();
        private readonly HashSet<IStaticZoneSortable> _staticSortables = new();
        private ZoneGraph? _graph;

        public int ZoneOrderStride => _zoneOrderStride;

        private void Awake()
        {
            if (_rebuildZonesOnAwake)
            {
                RebuildZones();
            }
        }

        public void Register(IDynamicZoneSortable sortable)
        {
            _dynamicSortables.Add(sortable);
        }

        public void Unregister(IDynamicZoneSortable sortable)
        {
            _dynamicSortables.Remove(sortable);
        }

        public void Register(IStaticZoneSortable sortable)
        {
            if (!_staticSortables.Add(sortable)) return;
            if (_graph == null) return;

            ApplyOrder(sortable, SortingLayer.NameToID(_zoneSortingLayer));
        }

        public void Unregister(IStaticZoneSortable sortable)
        {
            _staticSortables.Remove(sortable);
        }

        private void LateUpdate()
        {
            if (_graph == null) return;

            var layerId = SortingLayer.NameToID(_zoneSortingLayer);

            foreach (var sortable in _dynamicSortables)
            {
                ApplyOrder(sortable, layerId);
            }
        }

        public static void RebuildAllZones()
        {
            var service = FindAnyObjectByType<ZoneSortingService>();
            if (service == null)
            {
                Debug.LogWarning($"[{nameof(ZoneSortingService)}]: RebuildAllZones() found no active service in the scene.");
                return;
            }

            service.RebuildZones();
        }

        public void RebuildZones()
        {
            var sortingLines = FindObjectsByType<ZoneSortingLine>(FindObjectsSortMode.None);
            var validLines = sortingLines
                .Where(line => line.IsValid)
                .ToList();

            _graph = new ZoneGraph(validLines, _zoneOrderStride);

            var layerId = SortingLayer.NameToID(_zoneSortingLayer);
            foreach (var sortable in _staticSortables)
            {
                ApplyOrder(sortable, layerId);
            }

            Debug.Log($"[{nameof(ZoneSortingService)}]: Built zone graph with {validLines.Count} lines and {_graph.Zones.Count} zones", this);
        }

        private void ApplyOrder(IZoneSortable sortable, int layerId)
        {
            if (_graph == null) return;
            if (sortable.SortingGroup == null) return;

            sortable.SortingGroup.sortingLayerID = layerId;
            sortable.SortingGroup.sortingOrder = _graph.GetSortingOrderInLayer(sortable.SortPosition) + sortable.SortOrderBias;
        }
    }
}
