namespace IsometricZoneSorting
{
    /// <summary>
    /// Marker for sortables whose <see cref="IZoneSortable.SortPosition"/> is
    /// fixed for the lifetime of the current zone graph (walls, fences, doors,
    /// and other boundary geometry). The <see cref="IZoneSortingService"/>
    /// stamps these once at registration (if the graph exists) and again on
    /// every <c>RebuildZones()</c>, and skips them in the frame loop.
    /// For things that move, use <see cref="IDynamicZoneSortable"/>.
    /// </summary>
    public interface IStaticZoneSortable : IZoneSortable
    {
    }
}
