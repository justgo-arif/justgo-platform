using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.ResolveSessionId
{

    public class ResolveSessionIdQuery(int id) : IRequest<string>
    {
        public int Id { get; } = id;
    }
}
