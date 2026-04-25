using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.Logging
{
    public class SystemEvent
    {
        public int Id { get; set; }
        public int Category { get; set; }
        public int SubCategory { get; set; }
        public int Action { get; set; }
        public int ActionUserId { get; set; }
        public string ActionUserName { get; set; }
        public int AffectedEntityId { get; set; }
        public string AffectedEntityName { get; set; }
        public int AffectedEntityType { get; set; }
        public DateTime AuditDate { get; set; }
        public string Details { get; set; }
        public int OwningEntityId { get; set; }
        public string OwningEntityType { get; set; }
        public string ActionType { get; set; }
        public Guid? EventId { get; set; }
    }
}
