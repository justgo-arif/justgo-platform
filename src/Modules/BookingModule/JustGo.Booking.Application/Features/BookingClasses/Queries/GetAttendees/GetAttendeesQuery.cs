using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetAttendees
{
    public class GetAttendeesQuery(Guid sessionGuid) : IRequest<List<BookingAttendeeDto>>
    {
        public Guid SessionGuid { get; } = sessionGuid;
    }
}
