

using JustGo.AssetManagement.Domain.Entities.Enums;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class AssetOwnershipTransfer : RecordInfo
    {
        public int AssetOwnershipTransferId { get; set; }
        public int AssetId { get; set; }
        public DateTime TransferDate { get; set; }
        public string TransferNote { get; set; }
        public int TransferStatusId { get; set; }
        public int? PaymentId { get; set; }
        public int? OwnerClubId { get; set; }
        public RejectionReason RejectionReason { get; set; }
        public string RejectionNote { get; set; }
    }

}
