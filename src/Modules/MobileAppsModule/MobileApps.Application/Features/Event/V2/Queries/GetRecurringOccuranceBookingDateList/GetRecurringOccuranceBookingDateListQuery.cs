using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2.Event;

namespace MobileApps.Application.Features.Event.V2.Queries.GetRecurringOccuranceBookingDateList
{
    public class GetRecurringOccuranceBookingDateListQuery : IRequest<IList<BookingDate>>      
    {
        [Required]
        public int RowId { get; set; }         

    }
}
