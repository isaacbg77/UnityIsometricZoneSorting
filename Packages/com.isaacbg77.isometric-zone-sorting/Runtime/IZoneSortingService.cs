namespace IsometricZoneSorting
{
    public interface IZoneSortingService
    {
        /// <summary>
        /// Distance between adjacent zone boundaries. Boundaries live at
        /// <c>0, stride, 2·stride, …</c>; each zone's first sorting layer is one
        /// above its back boundary.
        /// </summary>
        public int ZoneOrderStride { get; }

        /// <summary>
        /// Register a dynamic sortable. Its order is re-resolved every
        /// <c>LateUpdate</c>.
        /// </summary>
        public void Register(IDynamicZoneSortable sortable);
        public void Unregister(IDynamicZoneSortable sortable);

        /// <summary>
        /// Register a static sortable. Its order is resolved once at registration
        /// (if the zone graph already exists) and again on every
        /// <c>RebuildZones()</c>, then left alone during the frame loop.
        /// </summary>
        public void Register(IStaticZoneSortable sortable);
        public void Unregister(IStaticZoneSortable sortable);
    }
}
