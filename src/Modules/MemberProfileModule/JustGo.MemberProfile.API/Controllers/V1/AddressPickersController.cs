using Asp.Versioning;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Application.Features.AddressPickers.Queries.GetAddressesByPostCode;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.MemberProfile.API.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/address-picker")]
[ApiController]
[Tags("Member Profile/Address Picker")]
public class AddressPickersController : ControllerBase
{
   readonly IMediator _mediator;
    public AddressPickersController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
    {
        _mediator = mediator;
    }

    [CustomAuthorize]
    [ProducesResponseType(typeof(ApiResponse<List<AddressDto>, object>), StatusCodes.Status200OK)]
    [HttpGet("addresses-by-post-code")]
    public async Task<IActionResult> GetAddressesByPostCode([FromQuery] GetAddressesByPostCodeQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }

}
