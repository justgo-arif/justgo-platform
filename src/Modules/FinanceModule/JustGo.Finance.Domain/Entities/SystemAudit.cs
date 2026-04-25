using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Finance.Domain.Entities
{
    public class SystemAudit
    {
        public int Id { get; set; }
        public int Category { get; set; }
        public int SubCategory { get; set; }
        public int Action { get; set; }
        public int ActionUserId { get; set; }
        public int AffectedEntityId { get; set; }
        public int AffectedEntityType { get; set; }
        public DateTime AuditDate { get; set; }
        public string Details { get; set; }
    }

}
