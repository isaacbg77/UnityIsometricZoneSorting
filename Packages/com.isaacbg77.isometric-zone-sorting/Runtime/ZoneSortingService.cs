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

        private readonly HashSet<IZoneSortable> _sortables = new();
        private ZoneGraph? _graph;

        public int ZoneOrderStride => _zoneOrderStride;
        
        private void Awake()
        {
            if (_rebuildZonesOnAwake)
            {
                RebuildZones();
            }
        }

        public void Register(IZoneSortable sortable)
        {
            _sortables.Add(sortable);
        }

        public void Unregister(IZoneSortable sortable)
        {
            _sortables.Remove(sortable);
        }

        private void LateUpdate()
        {
            if (_graph == null) return;

            var layerId = SortingLayer.NameToID(_zoneSortingLayer);

            foreach (var sortable in _sortables)
            {
                if (sortable.SortingGroup == null) continue;

                sortable.SortingGroup.sortingLayerID = layerId;
                sortable.SortingGroup.sortingOrder = _graph.GetSortingOrderInLayer(sortable.SortPosition) + sortable.SortOrderBias;
            }
        }

        public void RebuildZones()
        {
            var sortingLines = FindObjectsByType<ZoneSortingLine>(FindObjectsSortMode.None);
            var validLines = sortingLines
                .Where(line => line.IsValid)
                .ToList();

            _graph = new ZoneGraph(validLines, _zoneOrderStride);

            Debug.Log($"[{nameof(ZoneSortingService)}]: Built zone graph with {validLines.Count} lines and {_graph.Zones.Count} zones", this);
        }
    }
}
