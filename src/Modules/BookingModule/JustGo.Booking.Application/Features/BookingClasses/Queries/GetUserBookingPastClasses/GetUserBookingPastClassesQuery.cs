using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetUserBookingPastClasses;

public class GetUserBookingPastClassesQuery : IRequest<List<MemberClassDto>>
{
    public Guid UserGuid { get; set; }
    public Guid ClassGuid { get; set; }
    public GetUserBookingPastClassesQuery(Guid userId, Guid classId)
    {
        UserGuid = userId;
        ClassGuid = classId;
    }
}
