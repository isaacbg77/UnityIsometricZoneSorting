using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YoWorld.Core.Utilities.LayerUtils;

namespace YoWorld.Core.Sorting
{
    public class ZoneSortingService : MonoBehaviour, IZoneSortingService
    {
        [SerializeField] private SortingLayerReference? _dynamicSortingLayer;

        private readonly HashSet<IZoneSortable> _sortables = new();

        private ZoneGraph? _graph;

        // TODO: In production, this should listen to room manager events and rebuild
        // zones when a new room is loaded, using only lines from that room's scene.
        private void Awake()
        {
            RebuildZones();
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
            if (_graph == null || _dynamicSortingLayer == null) return;

            foreach (var sortable in _sortables)
            {
                if (sortable.SortingGroup == null) continue;

                sortable.SortingGroup.sortingLayerID = _dynamicSortingLayer.Id;
                sortable.SortingGroup.sortingOrder = _graph.GetSortingOrderInLayer(sortable.SortPosition);
            }
        }

        public void RebuildZones()
        {
            var sortingLines = FindObjectsByType<ZoneSortingLine>(FindObjectsSortMode.None);
            var validLines = sortingLines
                .Where(line => line.IsValid)
                .ToList();

            _graph = new ZoneGraph(validLines);

            Debug.Log($"[{nameof(ZoneSortingService)}]: Built zone graph with {validLines.Count} lines and {_graph.Zones.Count} zones", this);
        }
    }
}
