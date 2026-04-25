using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.ClassDetails;

public class GetClassDetailsQuery(Guid sessionGuid, string? inviteId) : IRequest<BookingClassDetailsDto?>
{
    public Guid SessionGuid { get; } = sessionGuid;
    public string? WaitlistHistoryId { get; } = inviteId;
}
