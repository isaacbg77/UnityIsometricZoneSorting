namespace IsometricZoneSorting
{
    /// <summary>
    /// Marker for sortables whose <see cref="IZoneSortable.SortPosition"/> can
    /// change frame-to-frame (characters, props, items). The
    /// <see cref="IZoneSortingService"/> re-resolves these every <c>LateUpdate</c>.
    /// For sortables that never move, use <see cref="IStaticZoneSortable"/>
    /// instead — they are stamped once per <c>RebuildZones()</c> and skipped
    /// during the frame loop.
    /// </summary>
    public interface IDynamicZoneSortable : IZoneSortable
    {
    }
}
