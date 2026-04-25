using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetAttendeePaymentForm;

public class GetAttendeePaymentFormQuery(int attendeeId) : IRequest<List<AttendeePaymentFormDto>>
{
    public int AttendeeId { get; } = attendeeId;
}
