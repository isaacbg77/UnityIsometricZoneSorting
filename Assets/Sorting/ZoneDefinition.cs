namespace YoWorld.Core.Sorting
{
    public readonly struct ZoneDefinition
    {
        public readonly int SortingOrderInLayer;
        public readonly ZoneSignature Signature;

        public ZoneDefinition(int sortingOrderInLayer, ZoneSignature signature)
        {
            SortingOrderInLayer = sortingOrderInLayer;
            Signature = signature;
        }
    }
}
