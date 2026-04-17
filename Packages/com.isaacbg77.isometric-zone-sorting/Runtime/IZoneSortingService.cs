namespace IsometricZoneSorting
{
    public interface IZoneSortingService
    {
        public void Register(IZoneSortable sortable);
        public void Unregister(IZoneSortable sortable);
    }
}
