using UnityEngine;

namespace IsometricZoneSorting
{
	public interface IZoneSortingService
	{
		/// <summary>
		/// Register a dynamic sortable. Its order is re-resolved every <c>LateUpdate</c>.
		/// </summary>
		public void Register(IDynamicZoneSortable zoneSortable);

		/// <summary>
		/// Unregister a dynamic sortable. Typically called on sortable's destruction.
		/// </summary>
		public void Unregister(IDynamicZoneSortable zoneSortable);

		/// <summary>
		/// Register a static sortable. Its order is resolved once at registration (if the zone graph already exists)
		/// and again on every <c>RebuildZones()</c>, then left alone during the frame loop.
		/// </summary>
		public void Register(IStaticZoneSortable zoneSortable);

		/// <summary>
		/// Unregister a static sortable. This should be called on the static sortable's destruction,
		/// e.g. on level change.
		/// </summary>
		public void Unregister(IStaticZoneSortable zoneSortable);
		
		/// <summary>
		/// Rebuilds the zone graph and re-resolves all sortables.
		/// </summary>
		public void RebuildZones(Transform? root = null);

		/// <summary>
		/// Adds appropriate sortable components to all renderers nested under the provided root transform. 
		/// </summary>
		public void AddSortableToRenderers(Transform? root = null);
	}
}
