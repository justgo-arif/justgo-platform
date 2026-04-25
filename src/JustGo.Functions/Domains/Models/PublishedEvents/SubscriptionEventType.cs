using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Functions.Domains.Models.PublishedEvents
{
    public class SubscriptionEventType
    {
        public int SubscriptioEventTypeId { get; set; }
        public int EventSubscriptionId { get; set; }
        public int EventTypeId { get; set; }
        public string EventMode { get; set; } = string.Empty;
    }
}
