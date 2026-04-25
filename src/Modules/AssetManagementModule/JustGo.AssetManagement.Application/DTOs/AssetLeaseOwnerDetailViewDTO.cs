namespace JustGo.AssetManagement.Application.DTOs
{
    public class AssetLeaseOwnerDetailViewDTO : AssetOwnerDetailViewDTO
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class AssetLeaseOwnerGridViewDTO : AssetLeaseOwnerDetailViewDTO
    {
        public string AssetLeaseId { get; set; }
    }
}
