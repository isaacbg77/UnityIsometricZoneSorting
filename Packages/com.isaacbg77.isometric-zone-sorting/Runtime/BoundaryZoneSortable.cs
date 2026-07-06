using System;
using UnityEngine;
using UnityEngine.Rendering;

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
			if (_pivot == null) Debug.LogError($"[{nameof(BoundaryZoneSortable)}]: {nameof(_pivot)} is not assigned", this);
			_sortingGroup = GetComponent<SortingGroup>();
			_renderers = GetComponentsInChildren<Renderer>();
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
#endif
	}
}
