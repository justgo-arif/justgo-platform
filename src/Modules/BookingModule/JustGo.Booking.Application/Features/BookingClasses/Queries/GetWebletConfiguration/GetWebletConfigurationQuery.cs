using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetWebletConfiguration
{
    public class GetWebletConfigurationQuery(Guid webletIdGuid) : IRequest<WebletConfigurationResponse?>
    {
        public Guid WebletIdGuid { get; } = webletIdGuid;
    }
}
