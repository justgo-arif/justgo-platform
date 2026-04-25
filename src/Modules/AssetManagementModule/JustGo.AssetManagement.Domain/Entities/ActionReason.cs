using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class ActionReason : BaseEntity
    {
        public int ActionReasonId { get; set; }
        public string ReasonName { get; set; }
    }
}
