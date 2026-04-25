using JustGo.AssetManagement.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class WorkflowStep : BaseEntity
    {
        public int StepId { get; set; }
        public int? ResourceId { get; set; }
        public int? AssetTypeId { get; set; }
        public string StepName { get; set; }
        public WorkFlowType WorkFlowType { get; set; } 
        public int StepOrder { get; set; }
        public AuthorityType AuthorityType { get; set; } 
        public int AuthorityId { get; set; }
    }

}
