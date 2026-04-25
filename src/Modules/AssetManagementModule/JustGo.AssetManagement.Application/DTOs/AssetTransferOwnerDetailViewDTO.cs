namespace JustGo.AssetManagement.Application.DTOs
{
    public class AssetTransferOwnerDetailViewDTO : AssetOwnerDetailViewDTO
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class AssetTransferOwnerGridViewDTO : AssetTransferOwnerDetailViewDTO
    {
        public string AssetTransferId { get; set; }
    }
}
