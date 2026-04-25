using JustGo.AssetManagement.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class EntityActionReason : RecordInfo
    {
        public int EntityActionReasonId {  get; set; }
        public int ActionReasonId { get; set; }
        public int ActionEntityId { get; set; }
        public ActionEntityType ActionEntityType { get; set; }
        public string ActionDescription  { get; set; }
        public ReasonStatus ReasonStatus { get; set; }
    }
}
