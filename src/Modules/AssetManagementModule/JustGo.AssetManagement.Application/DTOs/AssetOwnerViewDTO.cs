namespace JustGo.AssetManagement.Application.DTOs
{
    public class AssetOwnerViewDTO : AssetOwnerDTO
    {
        public string OwnerName { get; set; }
        public string ProfileImage { get; set; }
        public int OwnerDocId { get; set; }
        public string OwnerReferenceId { get; set; }
        public bool IsPrimary { get; set; }

    }

    public class AssetOwnerDetailViewDTO : AssetOwnerViewDTO
    {
        public string Email { get; set; }
    }
}
