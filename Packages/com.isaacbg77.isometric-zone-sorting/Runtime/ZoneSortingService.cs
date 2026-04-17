using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IsometricZoneSorting
{
    public class ZoneSortingService : MonoBehaviour, IZoneSortingService
    {
        [SerializeField, SortingLayer] private string _zoneSortingLayer = "Default";
        [SerializeField] private bool _rebuildZonesOnAwake = true;
        [SerializeField, Min(1), Tooltip("Gap between adjacent zones' sorting orders. Sortables sitting on a zone boundary can set SortOrderBias in [0, stride) to occupy an intermediate slot and never tie with movers.")]
        private int _zoneOrderStride = 10;

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
