using JustGo.AssetManagement.Domain.Entities.Enums;

namespace JustGo.AssetManagement.Application.DTOs
{
    public class WorkflowSubmissionDTO
    {
        public WorkFlowType WorkFlowType { get; set; }
        public string AssetTypeId { get; set; }
        public string EntityId { get; set; }
        public ActionStatus ActionStatus { get; set; }
        public RejectionReason RejectionReason { get; set; }
        public string Remarks { get; set; }
        public string? ProxyUserId { get; set; }
    }
}
