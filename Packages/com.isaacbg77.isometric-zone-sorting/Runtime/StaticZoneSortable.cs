using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace IsometricZoneSorting
{
	public class StaticZoneSortable : MonoBehaviour, IStaticZoneSortable
	{
		private SortingGroup? _sortingGroup;
		public SortingGroup? SortingGroup
		{
			get
			{
				if (_sortingGroup == null) _sortingGroup = GetComponent<SortingGroup>();
				return _sortingGroup;
			}
		}
		
		private Renderer[]? _renderers;
		public Renderer[]? Renderers
		{
			get
			{
				_renderers ??= GetComponentsInChildren<Renderer>();
				return _renderers;
			}
		}

		private Vector2 _sortPosition;
		public Vector2 SortPosition => _sortPosition;
		
		public event Action<IZoneSortable>? Destroyed;

		private IZoneSortingService? _zoneSortingService;

		private void Awake()
		{
			// Fetch the zone sorting service.
			_zoneSortingService = SceneUtils.FindInterfaceOfType<IZoneSortingService>();
			if (_zoneSortingService == null) Debug.LogError($"[{nameof(StaticZoneSortable)}]: {nameof(IZoneSortingService)} is null", this);
			
			_sortPosition = transform.position;
			_renderers = GetComponentsInChildren<Renderer>();
			_sortingGroup = GetComponent<SortingGroup>();
			
			if (_sortingGroup != null)
			{
				// We are sorting a sorting group. Ignore the presence of other renderers.
				// The sort position remains defined as the transform position.
				return;
			}

			// We need to handle TilemapRenderers differently from SpriteRenderers.
			// If we are sorting a tilemap, we define the sort position as the average position of all painted tiles.
			Tilemap? tilemap = GetComponent<Tilemap>();
			if (tilemap != null)
			{
				_sortPosition = ZoneSortingUtil.CalculateTilemapSortPosition(tilemap);
			}
		}
		
		private void OnDestroy()
		{
			Destroyed?.Invoke(this);
			Destroyed = null;
		}

#if UNITY_EDITOR
		private void OnDrawGizmosSelected()
		{
			Tilemap? tilemap = GetComponent<Tilemap>();
			if (tilemap == null) return;

			_sortPosition = ZoneSortingUtil.CalculateTilemapSortPosition(tilemap);

			Vector2 avgPosition = _sortPosition;
			Gizmos.color = Color.yellow;
			Gizmos.DrawSphere(avgPosition, 0.2f);
			Gizmos.DrawWireSphere(avgPosition, 0.4f);
		}
#endif // UNITY_EDITOR
	}
}
