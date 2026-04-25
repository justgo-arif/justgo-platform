using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities;

namespace MobileApps.Application.Features.Event.V2.Queries.ValidateRecurringBooking
{
    public class ValidateRecurringBookingQRQuery : IRequest<Tuple<IDictionary<string, object>,bool>>
    {
        [Required]
        public int DocId { get; set; }

    }
}
