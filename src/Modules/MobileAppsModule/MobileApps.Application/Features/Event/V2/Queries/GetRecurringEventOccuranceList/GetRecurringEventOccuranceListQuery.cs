using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Event.V2.Queries.GetRecurringEventOccuranceList
{
    public class GetRecurringEventOccuranceListQuery : IRequest<IList<IDictionary<string,object>>>      
    {
        [Required]
        public int EventDocId { get; set; }     
        public string Location { get; set; } = default!;
        public string? ScheduleStartDate { get; set; }   
        public string? ScheduleEndDate { get; set; }
        public bool? IsClassBooking { get; set; } = false;

    }
}
