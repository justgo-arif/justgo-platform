using Asp.Versioning;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.DTOs.ProfileBookingDtos;
using JustGo.Booking.Application.Features.ProfileCourseBooking.Commands.CancelCourseBooking;
using JustGo.Booking.Application.Features.ProfileCourseBooking.Queries.GetProfileCourseBooking;
using JustGo.Booking.Application.Features.ProfileCourseBooking.Queries.GetProfileCourseBookingQRLink;
using JustGo.Booking.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.Booking.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/profile-course-booking")]
[ApiController]
[Tags("Course Booking/Member Profile Booking")]

public class ProfileCourseBookingController :ControllerBase
{
    private readonly IMediator _mediator;
    public ProfileCourseBookingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [CustomAuthorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<ProfileCourseBookingGroupDto>, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfileBookings(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProfileBookingsQuery(id), cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }


    [CustomAuthorize]
    [ProducesResponseType(typeof(ApiResponse<int, object>), StatusCodes.Status200OK)]
    [HttpPatch("cancel-booking/{id:guid}")]
    public async Task<IActionResult> CancelCourseBooking(Guid id, CancellationToken cancellationToken)
    {
        var command = new CancelCourseBookingCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }


    [CustomAuthorize]
    [ProducesResponseType(typeof(ApiResponse<EventQRLink, object>), StatusCodes.Status200OK)]
    [HttpGet("booking-qr-link/{id:guid}")]
    public async Task<IActionResult> GetEventQRCodeLinkAsync(Guid id, CancellationToken cancellationToken)
    {
        return Ok(new ApiResponse<object, object>(await _mediator.Send(new GetProfileCourseBookingQRLinkQuery(id), cancellationToken)));
    }
}

