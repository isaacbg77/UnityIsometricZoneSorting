namespace YoWorld.Core.Sorting
{
    public interface IZoneSortingService
    {
        public void Register(IZoneSortable sortable);
        public void Unregister(IZoneSortable sortable);
    }
}
