using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace IsometricZoneSorting
{
	public interface IZoneSortable
	{
		public SortingGroup? SortingGroup { get; }
		public Renderer[]? Renderers { get; }
		public Vector2 SortPosition { get; }
		
		public event Action<IZoneSortable> Destroyed;
	}
}
