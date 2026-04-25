using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Functions.Domains.Models.PublishedEvents
{
    public class PublishedEvent
    {
        public long PublishedEventId { get; set; }        
        public int TenantId { get; set; }
        public int OrganisationId { get; set; }
        public int EventTypeId { get; set; }
        public string Payload { get; set; } = string.Empty;
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
