using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace IsometricZoneSorting
{
	public class ZoneSortingService : MonoBehaviour, IZoneSortingService
	{
		[SerializeField, SortingLayer] private string _zoneSortingLayer = "Default";
		private int _zoneSortingLayerId;

		[SerializeField] private bool _rebuildZonesOnAwake = true;

		private readonly HashSet<IDynamicZoneSortable> _dynamicSortables = new();
		private readonly HashSet<IStaticZoneSortable> _staticSortables = new();
		private ZoneGraph? _graph;

		private readonly int _zoneOrderStride = 1;

		private void Awake()
		{
			_zoneSortingLayerId = SortingLayer.NameToID(_zoneSortingLayer);
			if (_rebuildZonesOnAwake)
			{
				RebuildZones();
			}
		}

		public void Register(IDynamicZoneSortable zoneSortable)
		{
			if (!_dynamicSortables.Add(zoneSortable)) return;
			zoneSortable.Destroyed += Unregister;
			if (_graph != null) ApplyOrder(zoneSortable, _zoneSortingLayerId);
		}

		public void Unregister(IDynamicZoneSortable zoneSortable)
		{
			zoneSortable.Destroyed -= Unregister;
			_dynamicSortables.Remove(zoneSortable);
		}

		public void Register(IStaticZoneSortable zoneSortable)
		{
			if (!_staticSortables.Add(zoneSortable)) return;
			zoneSortable.Destroyed += Unregister;
			if (_graph != null) ApplyOrder(zoneSortable, _zoneSortingLayerId);
		}

		public void Unregister(IStaticZoneSortable zoneSortable)
		{
			zoneSortable.Destroyed -= Unregister;
			_staticSortables.Remove(zoneSortable);
		}

		private void Unregister(IZoneSortable zoneSortable)
		{
			switch (zoneSortable)
			{
				case IDynamicZoneSortable dynamicSortable:
					Unregister(dynamicSortable);
					break;
				case IStaticZoneSortable staticSortable:
					Unregister(staticSortable);
					break;
			}
		}

		private void LateUpdate()
		{
			if (_graph == null) return;

			foreach (var sortable in _dynamicSortables)
			{
				ApplyOrder(sortable, _zoneSortingLayerId);
			}
		}

		public void RebuildZones(Transform? root = null)
		{
			// Prune any inactive/destroyed sortables.
			// This can happen if an inactive sortable was destroyed without ever triggering its Awake(),
			// thus never triggering its OnDestroy().
			_staticSortables.RemoveWhere(s => s is not MonoBehaviour mb || mb == null);
			_dynamicSortables.RemoveWhere(s => s is not MonoBehaviour mb || mb == null);
			
			// Form a new graph based on the known boundary sortables.
			BoundaryZoneSortable[] boundarySortables;
			if (root == null)
			{
				boundarySortables = FindObjectsByType<BoundaryZoneSortable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			}
			else
			{
				boundarySortables = root.GetComponentsInChildren<BoundaryZoneSortable>(true);
			}
			_graph = new ZoneGraph(boundarySortables, _zoneOrderStride);
			
			// Now that we have a fresh graph, reset the cached pivot for all known dynamic sortables.
			// This is necessary because the pivots may have moved since the last time we sorted, esp on subsequent
			// calls to RebuildZones().
			foreach (var dynamicSortable in _dynamicSortables)
			{
				dynamicSortable.CachedPivotIndex = -1;
			}
			
			// Register existing sortables.
			StaticZoneSortable[] staticZoneSortables;
			if (root == null)
			{
				staticZoneSortables = FindObjectsByType<StaticZoneSortable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			}
			else
			{
				staticZoneSortables = root.GetComponentsInChildren<StaticZoneSortable>(true);
			}
			foreach (StaticZoneSortable sortable in staticZoneSortables)
			{
				Register(sortable);
			}
			
			// Apply the sorting order for all known sortables.
			for (int i = 0; i < _graph.PivotCount; i++)
			{
				BoundaryZoneSortable boundarySortable = _graph.GetSortedBoundary(i);
				ApplyOrder(boundarySortable, _zoneSortingLayerId, _graph.GetBaseSortingOrder(i));
			}
			foreach (var staticSortable in _staticSortables)
			{
				ApplyOrder(staticSortable, _zoneSortingLayerId);
			}
			foreach (var dynamicSortable in _dynamicSortables)
			{
				ApplyOrder(dynamicSortable, _zoneSortingLayerId);
			}
		}

		private void ApplyOrder(IZoneSortable zoneSortable, int layerId)
		{
			if (_graph == null)
			{
				Debug.LogError($"{nameof(ZoneSortingService)}.{nameof(ApplyOrder)}: Cannot apply sorting order to {zoneSortable} with no graph.", this);
				return;
			}

			int sortingOrder;
			if (zoneSortable is IDynamicZoneSortable dynamicSortable)
			{
				int cachedIndex = dynamicSortable.CachedPivotIndex;
				sortingOrder = _graph.GetSortingOrderInLayer(zoneSortable.SortPosition, ref cachedIndex);
				dynamicSortable.CachedPivotIndex = cachedIndex;
			}
			else
			{
				sortingOrder = _graph.GetSortingOrderInLayer(zoneSortable.SortPosition);
			}

			ApplyOrder(zoneSortable, layerId, sortingOrder);
		}

		private void ApplyOrder(IZoneSortable zoneSortable, int layerId, int sortingOrder)
		{
			if (zoneSortable.SortingGroup != null)
			{
				// Changing sorting order fields potentially every frame can be expensive,
				// so we check against the values needing to change first.
				if (zoneSortable.SortingGroup.sortingLayerID != layerId) zoneSortable.SortingGroup.sortingLayerID = layerId;
				if (zoneSortable.SortingGroup.sortingOrder != sortingOrder) zoneSortable.SortingGroup.sortingOrder = sortingOrder;
				return;
			}

			// The sortable isn't being overridden by a SortingGroup, so we'll apply it directly to the renderer.
			Renderer[]? sortableRenderers = zoneSortable.Renderers;
			if (sortableRenderers == null) return;
			foreach (Renderer r in sortableRenderers)
			{
				// Changing sorting order fields potentially every frame can be expensive,
				// so we check against the values needing to change first.
				if (r.sortingLayerID != layerId) r.sortingLayerID = layerId;
				if (r.sortingOrder != sortingOrder) r.sortingOrder = sortingOrder;
			}
		}

		public void AddSortableToRenderers(Transform? root = null)
		{
			SortingGroup[]? sortingGroups;
			SpriteRenderer[]? spriteRenderers;
			TilemapRenderer[]? tileMapRenderers;

			// Find all render components that have a sorting layer set.
			if (root == null)
			{
				sortingGroups = FindObjectsByType<SortingGroup>(FindObjectsSortMode.None);
				spriteRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
				tileMapRenderers = FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
			}
			else
			{
				sortingGroups = root.GetComponentsInChildren<SortingGroup>();
				spriteRenderers = root.GetComponentsInChildren<SpriteRenderer>();
				tileMapRenderers = root.GetComponentsInChildren<TilemapRenderer>();
			}

			List<SpriteRenderer> validSpriteRenderers = spriteRenderers.Where(sr => sr.sortingLayerID == _zoneSortingLayerId &&
			                                                                        sr.GetComponentInParent<SortingGroup>() == null &&
			                                                                        sr.GetComponent<IZoneSortable>() == null).ToList();
			List<TilemapRenderer> validTileMapRenderers = tileMapRenderers.Where(tm => tm.sortingLayerID == _zoneSortingLayerId &&
			                                                                           tm.GetComponentInParent<SortingGroup>() == null &&
			                                                                           tm.GetComponent<IZoneSortable>() == null).ToList();
			List<SortingGroup> validSortingGroups = sortingGroups.Where(sg => sg.sortingLayerID == _zoneSortingLayerId &&
			                                                                  sg.GetComponent<IZoneSortable>() == null).ToList();

			AddSortableSiblingComponent(validSpriteRenderers);
			AddSortableSiblingComponent(validTileMapRenderers);
			AddSortableSiblingComponent(validSortingGroups);
		}

		private void AddSortableSiblingComponent<T>(List<T> components) where T : Component
		{
			foreach (T script in components)
			{
				var sortable = script.gameObject.AddComponent<StaticZoneSortable>();
				Register(sortable);
			}
		}
	}
}
