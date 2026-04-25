using Asp.Versioning;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.Features.ClassManagement.Queries.GetAttendeeOccurenceCalendarView;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using JustGo.Booking.Application.DTOs.ClassManagementDtos;
using JustGo.Booking.Application.Features.ClassManagement.Queries.ProRataCalculation;

namespace JustGo.Booking.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/class-management")]
[ApiController]
[Tags("Class Management")]
public class ClassManagementController : ControllerBase
{
    private readonly IMediator _mediator;
    public ClassManagementController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [CustomAuthorize]
    [HttpPost("get-attendee-occurrence-calendar-view")]
    public async Task<Result<CalendarViewResponseDto>> GetAttendeeOccurenceCalendarView([FromBody] SessionCalendarViewRequest request)
    {
        if (request.PageNumber < 1) request.PageNumber = 1;
        if (request.RowsPerPage < 1) request.RowsPerPage = 20;

        var response = await _mediator.Send(new GetAttendeeOccurenceCalendarViewQuery(request));
        return response;
    }
    [CustomAuthorize]
    [HttpPost("pro-rata-calculation")]
    public async Task<Result<object>> ProRataCalculation([FromBody] ProRataCalculationRequestDto request)
    {
        var response = await _mediator.Send(new ProRataCalculationQuery(request));
        return response;
    }
}