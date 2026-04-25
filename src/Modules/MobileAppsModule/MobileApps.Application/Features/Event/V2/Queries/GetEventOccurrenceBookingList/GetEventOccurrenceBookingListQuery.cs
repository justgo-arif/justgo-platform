using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.BaseEntity;

namespace MobileApps.Application.Features.Event.V2.Queries.GetEventOccurrenceBookingList
{
    public class GetEventOccurrenceBookingListQuery : IBasePaging, IRequest<IList<IDictionary<string,object>>>
    {
        [Required]
        public int OccuranceRowId { get; set; }        
        public string AttendeeName { get; set; } = default!;
        public List<int> TicketTypes { get; set; } = new List<int>();
        public List<string> AttendeeStatuses { get; set; } = new List<string>();
        [Required]
        public string OccuranceDate { get; set; }
        public int NextId { get; set; }
        public int DataSize { get; set; }
        public string? SortOrder { get ; set; }
    }
}
