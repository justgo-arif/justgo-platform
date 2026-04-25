using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Event.V2.Queries.ValidateBooking    
{
    public class ValidateBookingQuery : IRequest<Dictionary<string, object>>
    {
        [Required]
        public Guid MemberSyncGuid { get; set; }
        [Required]
        public Guid DocumentSyncGuid { get; set; }
        [Required]
        public int DocId { get; set; }
        [Required]
        public DateTime BookingDate { get; set; }
        public DateTime CheckedInAt { get; set; } = DateTime.UtcNow;

    }
}

