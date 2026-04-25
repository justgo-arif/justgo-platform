using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetOccurrences;

public class GetOccurrencesQuery(Guid sessionGuid) : IRequest<List<BookingOccurrenceDto>>
{
    public Guid SessionGuid { get; } = sessionGuid;
}
