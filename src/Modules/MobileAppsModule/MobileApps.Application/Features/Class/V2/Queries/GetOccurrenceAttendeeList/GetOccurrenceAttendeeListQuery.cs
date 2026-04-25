using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Class.V2.Queries.GetOccurrenceAttendeeList
{
    public class GetOccurrenceAttendeeListQuery:IRequest<IList<IDictionary<string,object>>>
    {
        public int OccurrenceId { get; set; }
        public string? AttendeeName { get; set; }
        public List<int> TicketTypes { get; set; } = new List<int>();
        public List<string> AttendeeStatuses { get; set; } = new List<string>();
    }
}
