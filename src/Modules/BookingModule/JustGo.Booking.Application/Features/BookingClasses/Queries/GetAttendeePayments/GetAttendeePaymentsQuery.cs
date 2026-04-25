using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetAttendeePayments;

public class GetAttendeePaymentsQuery(Guid sessionGuid) : IRequest<List<GroupedBookingAttendeePaymentDto>>
{
    public Guid SessionGuid { get; } = sessionGuid;
}
