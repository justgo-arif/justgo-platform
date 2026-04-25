namespace JustGo.AssetManagement.Application.DTOs
{
    public class LeaseDTOWithRawData : LeaseListItemDTO
    {
        public int LeaseId { get; set; }
        public int AssetId { get; set; }   
        public int RowIndex { get; set; }
        public string Owners { get; set; }
        public string Images { get; set; }

    }


}
