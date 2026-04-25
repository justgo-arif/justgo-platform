using Asp.Versioning;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetUserBookingClasses;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetUserBookingPastClasses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.Booking.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/profile-booking")]
[ApiController]
[Tags("Class Booking/Member Profile Booking")]
public class ProfileClassBookingController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProfileClassBookingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [CustomAuthorize]
    [HttpGet("classes")]
    [ProducesResponseType(typeof(ApiResponse<KeysetPagedResult<MemberClassDto>, object>), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client, VaryByHeader = "Authorization")]
    public async Task<IActionResult> GetUserBookingClasses([FromQuery] GetUserBookingClassesQuery request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        return Ok(new ApiResponse<KeysetPagedResult<MemberClassDto>, object>(result));
    }

    [CustomAuthorize]
    [HttpGet("{userId:guid:required}/past-classes/{classId:guid:required}")]
    [ProducesResponseType(typeof(ApiResponse<List<MemberClassDto>, object>), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client, VaryByHeader = "Authorization")]
    public async Task<IActionResult> GetUserBookingPastClasses([FromRoute] Guid userId, [FromRoute] Guid classId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserBookingPastClassesQuery(userId, classId), cancellationToken);
        return Ok(new ApiResponse<List<MemberClassDto>, object>(result));
    }
}
