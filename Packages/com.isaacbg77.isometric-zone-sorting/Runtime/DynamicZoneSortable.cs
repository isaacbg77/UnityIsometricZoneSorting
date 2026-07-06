using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace IsometricZoneSorting
{
	/// <summary>
	/// Default <see cref="IDynamicZoneSortable"/> for things that move (e.g. characters).
	/// <see cref="SortPosition"/> tracks <c>transform.position</c> each frame and the
	/// service re-resolves the sorting order every <c>LateUpdate</c>.
	/// </summary>
	[RequireComponent(typeof(SortingGroup))]
	public class DynamicZoneSortable : MonoBehaviour, IDynamicZoneSortable
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
		
		public Vector2 SortPosition => transform.position;
		
		public event Action<IZoneSortable>? Destroyed;

		public int CachedPivotIndex { get; set; } = -1;
		
		private IZoneSortingService? _zoneSortingService;
		

		private void Awake()
		{
			_zoneSortingService = SceneUtils.FindInterfaceOfType<IZoneSortingService>();
			if (_zoneSortingService == null) Debug.LogError($"[{nameof(DynamicZoneSortable)}]: {nameof(IZoneSortingService)} is null", this);
			if (_zoneSortingService != null) _zoneSortingService.Register(this);
			_sortingGroup = GetComponent<SortingGroup>();
			_renderers = GetComponentsInChildren<Renderer>();
		}
		
		private void OnDestroy()
		{
			if (_zoneSortingService != null) _zoneSortingService.Unregister(this);
			Destroyed?.Invoke(this);
			Destroyed = null;
		}
	}
}
