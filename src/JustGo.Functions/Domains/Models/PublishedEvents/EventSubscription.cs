using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Functions.Domains.Models.PublishedEvents
{
    public class EventSubscription
    {
        public int EventSubscriptionId { get; set; }
        public string? OrganisationId { get; set; }
        public string? OrganisationName { get; set; }
        public int OrganisationType { get; set; }
        public string? UserId { get; set; }
        public string EndpointUrl { get; set; } = string.Empty;
        public string? SecretKey { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
