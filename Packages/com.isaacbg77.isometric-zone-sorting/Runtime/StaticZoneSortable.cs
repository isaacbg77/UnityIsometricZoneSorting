using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace IsometricZoneSorting
{
	/// <summary>
	/// <see cref="IStaticZoneSortable"/> for static objects that do not sit on a sorting line
	/// (objects facing the camera with no orthographic structure elongated along a single diagonal).
	/// <see cref="BoundaryZoneSortable"/> is a more general case for objects that sit on a line."
	/// </summary>
	[RequireComponent(typeof(SortingGroup))]
	public class StaticZoneSortable : MonoBehaviour, IStaticZoneSortable
	{
		[SerializeField, Min(0), Tooltip("Offset added to the zone's first sorting layer. " +
		                                 "0 (default) puts this sortable on the first layer in its zone; " +
		                                 "raise it to stack above other movers within the same zone. " +
		                                 "Must be less than stride-1 to stay inside the zone; stride-1 lands on the zone's front boundary.")]
		private int _sortOrderBias;

		private IZoneSortingService? _zoneSortingService;
		private SortingGroup? _sortingGroup;

		public SortingGroup SortingGroup => _sortingGroup ?? throw new NullReferenceException();
		public Vector2 SortPosition => transform.position;
		public int SortOrderBias => _sortOrderBias;

		private void Awake()
		{
			_zoneSortingService = SceneUtils.FindInterfaceOfType<IZoneSortingService>();
			if (_zoneSortingService == null) Debug.LogError($"[{nameof(DynamicZoneSortable)}]: {nameof(IZoneSortingService)} is null", this);

			_sortingGroup = GetComponent<SortingGroup>();
			if (_sortingGroup == null) Debug.LogError($"[{nameof(DynamicZoneSortable)}]: {nameof(_sortingGroup)} is null", this);
		}

		private void OnEnable()
		{
			if (_zoneSortingService == null || _sortingGroup == null) return;
			_zoneSortingService.Register(this);
		}

		private void OnDisable()
		{
			_zoneSortingService?.Unregister(this);
		}
		
	}
}
