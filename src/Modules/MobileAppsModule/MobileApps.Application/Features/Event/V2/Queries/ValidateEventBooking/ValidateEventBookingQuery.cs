using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Event.V2.Queries.ValidateEventBooking    
{
    public class ValidateEventBookingQuery : IRequest<Tuple<IDictionary<string, object>, bool>>
    {
        [Required]
        public int DocId { get; set; }
        public DateTime CheckedInAt { get; set; }
        public DateTime BookingDate { get; set; }
    }
}
