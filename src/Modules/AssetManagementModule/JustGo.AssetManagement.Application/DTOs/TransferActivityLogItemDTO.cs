using JustGo.AssetManagement.Domain.Entities.Enums;

namespace JustGo.AssetManagement.Application.DTOs
{
    public class TransferActivityLogItemDTO
    {

        public Guid ActionId { get; set; }
        public string ActionName { get; set; }
        public DateTime ActionDate { get; set; }
        public string ActionUserFullName { get; set; }
        public Guid ActionUserId { get; set; }
        public int ActionUserDocId { get; set; }
        public string ActionUserMemberId { get; set; }
        public string ActionUserImage { get; set; }
        public RejectionReason RejectionReason { get; set; }
        public string RejectionNote { get; set; }


    }

    public class TransferActivityLogRawItemDTO : TransferActivityLogItemDTO
    {
        public int TotalRows { get; set; }

    }
}
