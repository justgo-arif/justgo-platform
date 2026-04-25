using JustGo.AssetManagement.Domain.Entities.Enums;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class WorkflowEntity : RecordInfo
    {
        public int WorkflowEntityId { get; set; }
        public int StepId { get; set; }
        public int EntityId { get; set; }
        public int UserId { get; set; }
        public DateTime ActionDate { get; set; }
        public ActionStatus ActionStatus { get; set; } 
        public RejectionReason RejectionReason { get; set; }
        public string Remarks { get; set; }
    }

}
