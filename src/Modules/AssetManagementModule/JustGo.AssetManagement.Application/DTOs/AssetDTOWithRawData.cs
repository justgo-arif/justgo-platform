namespace JustGo.AssetManagement.Application.DTOs
{

    public class AssetDTOWithRawData : AssetListItemDTO
    {
        public int TotalRows { get; set; }
        public int RowIndex { get; set; }
        public int AssetId { get; set; }
        public string Owners { get; set; }
        public string Images { get; set; }
        public string Tags { get; set; }
        public string PrimaryLicenseInfo { get; set; }
        public string AdditionalLicenseInfo { get; set; }
        public int AssetDocCode { get; set; }
        public string Barcode { get; set; }

    }

    
}
