namespace JustGo.AssetManagement.Domain.Entities
{
    public class AssetTypesCredentialsLink : BaseEntity
    {
        public int CredentialLinkId { get; set; }
        public int AssetTypeId { get; set; }
        public int CredentialsDocid { get; set; }
    }

}
