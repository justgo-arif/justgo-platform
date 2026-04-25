using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Functions.Domains.Models.PublishedEvents
{
    public class PublishedEventResponses
    {
        public long PublishedEventResponseId { get; set; }
        public long PublishedEventId { get; set; }
        public int EventSubscriptionId { get; set; }
        public int Status { get; set; }
        public string ResponseBody { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

    }
}
