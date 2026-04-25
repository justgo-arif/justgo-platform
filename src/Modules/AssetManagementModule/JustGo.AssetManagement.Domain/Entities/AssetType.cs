

namespace JustGo.AssetManagement.Domain.Entities
{
    public class AssetType : BaseEntity
    {
        public int AssetTypeId { get; set; }
        public string TypeName { get; set; }
        public string AssetApprovalConfig { get; set; }
        public string AssetRegistrationConfig { get; set; }
        public string AssetRetentionConfig { get; set; }
        public string AssetLeaseConfig { get; set; }
        public string AssetTransferConfig { get; set; }
        public int? DigitalWalletTemplateId { get; set; }
        public string AssetTypeConfig { get; set; }
        public int TenantId { get; set; }
    }

}
