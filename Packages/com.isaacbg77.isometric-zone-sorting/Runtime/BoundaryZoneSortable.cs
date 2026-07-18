using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace IsometricZoneSorting
{
	/// <summary>
	/// <see cref="IStaticZoneSortable"/> for static objects that sit on a sorting pivot
	/// (walls, fences, doors, railings). <see cref="SortPosition"/> is derived from the
	/// referenced <see cref="ZoneSortingPivot"/>.
	/// </summary>
	public class BoundaryZoneSortable : MonoBehaviour, IBoundaryZoneSortable
	{
		[SerializeField, Tooltip("The sorting pivot this object sits on. SortPosition is the pivot's position, offset slightly away from its front normals.")]
		private ZoneSortingPivot? _pivot;
		private ZoneSortingPivot? Pivot
		{
			get
			{
				if (_pivot == null) _pivot = GetComponentInChildren<ZoneSortingPivot>();
				return _pivot;
			}
		}

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

		public Vector2 SortPosition
		{
			get
			{
				if (Pivot == null) return transform.position;
				return Pivot.Position;
			}
		}

		public event Action<IZoneSortable>? Destroyed;

		public ZoneSortingPivot.SortingAxes SortingAxes => Pivot == null ? ZoneSortingPivot.SortingAxes.Default() : Pivot.GetSortingAxes();
		
		private void Awake()
		{
			_sortingGroup = GetComponent<SortingGroup>();
			_renderers = GetComponentsInChildren<Renderer>();

			if (Pivot != null)
			{
				// Record our sort position prior to any shifts in the tilemap sort pivots. 
				Vector3 sortPosition = SortPosition;
				foreach (Renderer r in _renderers)
				{
					var tilemapRenderer = r as TilemapRenderer;
					if (r == null || tilemapRenderer == null) continue;
					var tilemap = tilemapRenderer.GetComponent<Tilemap>();
					ZoneSortingUtil.SetTilemapSortPivot(tilemap, SortPosition);
				}
				
				// Restore the original sort position, in the event it was changed.
				Pivot.transform.position = sortPosition;
			}
			else
			{
				Debug.LogError($"[{nameof(BoundaryZoneSortable)}]: {nameof(_pivot)} is not assigned", this);
			}
		}

		private void OnDestroy()
		{
			Destroyed?.Invoke(this);
			Destroyed = null;
		}

#if UNITY_EDITOR
		private void Reset()
		{
			// Avoid creating a duplicate if one already exists.
			_pivot = GetComponentInChildren<ZoneSortingPivot>();
			if (_pivot == null)
			{
				var pivot = new GameObject("SortingPivot");
				pivot.transform.SetParent(transform, worldPositionStays: false);
				_pivot = pivot.AddComponent<ZoneSortingPivot>();

				// Mark the new child as part of the undo history so it can be undone
				UnityEditor.Undo.RegisterCreatedObjectUndo(pivot, "Add SortingPivot");
				UnityEditor.EditorUtility.SetDirty(this);
			}
		}
		
		private void OnDrawGizmosSelected()
		{
			// Draw each tilemap's transparency sort point — the renderer bounds center, which
			// ZoneSortingUtil.SetTilemapSortPivot aligns to the sorting pivot at runtime.
			Gizmos.color = Color.yellow;
			foreach (TilemapRenderer tilemapRenderer in GetComponentsInChildren<TilemapRenderer>())
			{
				Vector2 sortPoint = tilemapRenderer.bounds.center;
				Gizmos.DrawSphere(sortPoint, 0.2f);
				Gizmos.DrawWireSphere(sortPoint, 0.4f);
			}
		}
#endif // UNITY_EDITOR
	}
}
