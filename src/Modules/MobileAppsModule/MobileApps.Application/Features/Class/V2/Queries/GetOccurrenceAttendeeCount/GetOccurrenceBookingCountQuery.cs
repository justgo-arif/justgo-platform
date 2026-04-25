using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Class.V2.Queries.GetOccurrenceBookingCount
{
    public class GetOccurrenceBookingCountQuery:IRequest<IList<IDictionary<string, object>>>
    {
        public int OccurrenceId { get; set; }
        public string? AttendeeName { get; set; }
        public int? TicketType { get; set; }
        public string? AttendeeStatus { get; set; }
    }
}
