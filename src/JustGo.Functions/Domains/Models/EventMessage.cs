using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Functions.Domains.Models
{
    public class EventMessage
    {
        public long EventId { get; set; }
        public int EventTypeId { get; set; }
        public int TenantId { get; set; }
        public int OrganisationId { get; set; }
        public string Payload { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int Status { get; set; }
    }
}
