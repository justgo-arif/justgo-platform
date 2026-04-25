using Asp.Versioning;
using Azure.Core;
using JustGo.Authentication.Helper.Attributes;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Infrastructure.CustomCors;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Booking.Application.Features.BookingClasses.Commands.UserInvitation;
using JustGo.Booking.Application.Features.BookingClasses.Queries.AttendeeListByOccurence;
using JustGo.Booking.Application.Features.BookingClasses.Queries.ClassDetails;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetAttendeePaymentForm;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetAttendeePayments;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetAttendees;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetClasses;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetClassGroupDetails;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetOccurrences;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetPricingChartDropdown;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetPrimaryClubGuid;
using JustGo.Booking.Application.Features.BookingClasses.Queries.ResolveSessionId;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.Booking.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/booking-class")]
//[EnableCors(CorsConfiguration.WebletPolicy)]
[ApiController]
[Tags("Class Booking/Class")]
[TenantFromHeader]
public class BookingClassController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICustomError _error;
    public BookingClassController(IMediator mediator, ICustomError error)
    {
        _mediator = mediator;
        _error = error;
    }

    [AllowAnonymous]
    [HttpPost("classes")]
    [ProducesResponseType(typeof(ApiResponse<KeysetPagedResult<BookingClassDto>, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClassesBySyncGuid([FromBody] GetClassesBySyncGuidQuery request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        return Ok(new ApiResponse<KeysetPagedResult<BookingClassDto>, object>(result));
    }

    [AllowAnonymous]
    [HttpGet("class-details/{id:guid:required}")]
    [ProducesResponseType(typeof(ApiResponse<BookingClassDetailsDto, object>), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id,Authorization")]
    public async Task<IActionResult> GetClassDetails([FromRoute] Guid id, [FromQuery] string? inviteId, CancellationToken cancellationToken)
    {
        if (id.Equals(Guid.Empty))
        {
            _error.CustomValidation<object>("Invalid request.");
            return new EmptyResult();
        }
        var result = await _mediator.Send(new GetClassDetailsQuery(id, inviteId), cancellationToken);
        if (result is null)
        {
            _error.NotFound<object>("Class details not found.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<BookingClassDetailsDto, object>(result));
    }

    [AllowAnonymous]
    [HttpGet("occurrence-details/{id:guid:required}")]
    [ProducesResponseType(typeof(ApiResponse<List<BookingOccurrenceDto>, object>), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id")]
    public async Task<IActionResult> GetOccurrences([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (id.Equals(Guid.Empty))
        {
            _error.CustomValidation<object>("Invalid request.");
            return new EmptyResult();
        }
        var result = await _mediator.Send(new GetOccurrencesQuery(id), cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }

    [CustomAuthorize]
    [HttpGet("attendees/{id:guid:required}")]
    [ProducesResponseType(typeof(ApiResponse<List<BookingAttendeeDto>, object>), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id")]
    public async Task<IActionResult> GetAttendees([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (id.Equals(Guid.Empty))
        {
            _error.CustomValidation<object>("Invalid request.");
            return new EmptyResult();
        }
        var result = await _mediator.Send(new GetAttendeesQuery(id), cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }

    [CustomAuthorize]
    [HttpGet("attendee-payments/{id:guid:required}")]
    [ProducesResponseType(typeof(ApiResponse<List<GroupedBookingAttendeePaymentDto>, object>), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id")]
    public async Task<IActionResult> GetAttendeePayments([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (id.Equals(Guid.Empty))
        {
            _error.CustomValidation<object>("Invalid request.");
            return new EmptyResult();
        }
        var result = await _mediator.Send(new GetAttendeePaymentsQuery(id), cancellationToken);
        if (result is null || result.Count == 0)
        {
            _error.NotFound<object>("Payments not found.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<object, object>(result));
    }

    [CustomAuthorize]
    [HttpGet("payment-form/{attendeeId:int:min(1)}")]
    [ProducesResponseType(typeof(ApiResponse<List<AttendeePaymentFormDto>, object>), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id")]
    public async Task<IActionResult> GetAttendeePaymentForm([FromRoute] int attendeeId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAttendeePaymentFormQuery(attendeeId), cancellationToken);
        if (result is null || result.Count == 0)
        {
            _error.NotFound<object>("No data found.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<List<AttendeePaymentFormDto>, object>(result));
    }

    [AllowAnonymous]
    [HttpGet("class-group-details/{id:guid:required}")]
    [ProducesResponseType(typeof(ApiResponse<BookingClassGroupDetailsDto, object>), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id")]
    public async Task<IActionResult> GetClassGroupDetails([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (id.Equals(Guid.Empty))
        {
            _error.CustomValidation<object>("Invalid request.");
            return new EmptyResult();
        }
        var result = await _mediator.Send(new GetClassGroupDetailsQuery(id), cancellationToken);
        if (result is null)
        {
            _error.NotFound<object>("Class details not found.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<BookingClassGroupDetailsDto, object>(result));
    }

    [CustomAuthorize]
    [HttpGet("primary-club-guid")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id")]
    public async Task<IActionResult> GetPrimaryClubGuid(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPrimaryClubGuidQuery(), cancellationToken);
        if (result is null)
        {
            _error.NotFound<object>("No primary club found and no ngb found.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<object, object>(new { ClubGuid = result }));
    }

    [CustomAuthorize]
    [HttpPut("invite-user/{inviteId:guid:required}")]
    public async Task<IActionResult> UserInvitation([FromRoute] Guid inviteId, CancellationToken cancellationToken)
    {
        int affectedRows = await _mediator.Send(new UserInvitationCommand(inviteId), cancellationToken);

        if (affectedRows <= 0)
        {
            return Ok(new ApiResponse<object, object>(new { IsUserSynced = false }));
        }

        return Ok(new ApiResponse<object, object>(new { IsUserSynced = true }));
    }

    [CustomAuthorize]
    [HttpGet("attendee_list")]
    [ProducesResponseType(typeof(ApiResponse<SessionAttendanceResponseDto, object>), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id")]
    public async Task<IActionResult> GetAttendeeListByOccurence([FromQuery] 
        Guid sessionGuid,
        Guid ownerGuid,
        int occurrenceId,
        int rowsPerPage,
        int pageNumber,
        bool isActiveMemberOnly, 
        string? filterValue ,
        CancellationToken cancellationToken)
    {
        if (sessionGuid.Equals(Guid.Empty) || ownerGuid.Equals(Guid.Empty))
        {
            _error.CustomValidation<object>("Invalid request.");
            return new EmptyResult();
        }
        if (pageNumber < 1 || rowsPerPage < 1)
        {
            throw new ArgumentException("PageNumber and RowsPerPage must be greater than 0");
        }
        var result = await _mediator.Send(new GetAttendeeListByOccurenceQuery( sessionGuid, ownerGuid, occurrenceId,  rowsPerPage,pageNumber, isActiveMemberOnly, filterValue), cancellationToken);
        return Ok(new ApiResponse<SessionAttendanceResponseDto, object>(result));
    }

    [CustomAuthorize]
    [HttpGet("resolve-session-id")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id")]
    public async Task<IActionResult> ResolveSessionId([FromQuery] int id,CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ResolveSessionIdQuery(id), cancellationToken);
        return Ok(new ApiResponse<string, object>(result));
    }
}