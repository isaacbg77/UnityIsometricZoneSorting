namespace IsometricZoneSorting
{
	public interface IDynamicZoneSortable : IZoneSortable
	{
		public int CachedPivotIndex { get; set; }
	}
}
