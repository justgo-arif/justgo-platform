
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.DTOs.ClassManagementDtos;

namespace JustGo.Booking.Application.Features.ClassManagement.Queries.GetAttendeeOccurenceCalendarView
{
    public class GetAttendeeOccurenceCalendarViewQuery(SessionCalendarViewRequest calendarRequest)
        : IRequest<Result<CalendarViewResponseDto>>
    {
        public SessionCalendarViewRequest CalendarRequest { get; set; } = calendarRequest;
    }
}
