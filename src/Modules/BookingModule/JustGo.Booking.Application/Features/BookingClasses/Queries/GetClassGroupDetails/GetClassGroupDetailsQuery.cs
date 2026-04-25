using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetClassGroupDetails;

public class GetClassGroupDetailsQuery(Guid classGuid) : IRequest<BookingClassGroupDetailsDto?>
{
    public Guid ClassGuid { get; } = classGuid;
}
