namespace JustGo.AssetManagement.Application.DTOs
{

    public class ActionRequiredItemDTO
    {
        public string AssetRegisterId { get; set; }
        public string AssetLeaseId { get; set; }
        public string AssetTransferId { get; set; }
        public int? LicenseType { get; set; }
        public string AssetReference { get; set; }
        public string AssetName { get; set; }
        public List<AssetOwnerViewDTO> AssetOwners { get; set; }
        public List<AssetImageDTO> AssetImages { get; set; }
        public string Reason { get; set; }
        public string Description { get; set; }

 
    }
    public class ActionRequiredRawItemDTO
    {

        public int TotalRows { get; set; }
        public int AssetId { get; set; }
        public string AssetRegisterId { get; set; }
        public string AssetLeaseId { get; set; }
        public string AssetTransferId { get; set; }
        public int? LicenseType { get; set; }
        public string AssetReference { get; set; }
        public string AssetName { get; set; }
        public string Owners { get; set; }
        public string Images { get; set; }
        public string OwnerName { get; set; }
        public string ProfileImage { get; set; }
        public string Reason { get; set; }
        public string Description { get; set; }


    }
}
