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

        public void Register(IZoneSortable sortable);
        public void Unregister(IZoneSortable sortable);
    }
}
