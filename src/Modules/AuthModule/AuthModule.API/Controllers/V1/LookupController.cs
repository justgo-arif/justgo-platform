using Asp.Versioning;
using AuthModule.Application.DTOs.Lookup;
using AuthModule.Application.Features.Lookup.Queries.GetClubTypes;
using AuthModule.Application.Features.Lookup.Queries.GetCountrys;
using AuthModule.Application.Features.Lookup.Queries.GetCountys;
using AuthModule.Application.Features.Lookup.Queries.GetGender;
using AuthModule.Application.Features.Lookup.Queries.GetRegions;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthModule.API.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/lookup")]
[ApiController]
[Tags("Authentication/Lookup")]
public class LookupController : ControllerBase
{
    IMediator _mediator;
    private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;
    public LookupController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
    {
        _mediator = mediator;
        _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
    }

    [CustomAuthorize]
    [ProducesResponseType(typeof(ApiResponse<List<SelectListItemDTO<string>>, object>), StatusCodes.Status200OK)]
    [HttpGet("countrys")]
    public async Task<IActionResult> GetCountrys(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCountrysQuery(), cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }

    [CustomAuthorize]
    [ProducesResponseType(typeof(ApiResponse<List<SelectListItemDTO<string>>, object>), StatusCodes.Status200OK)]
    [HttpGet("countys")]
    public async Task<IActionResult> GetCountys(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCountysQuery(), cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }

    [CustomAuthorize]
    [ProducesResponseType(typeof(ApiResponse<List<SelectListItemDTO<string>>, object>), StatusCodes.Status200OK)]
    [HttpGet("gender")]
    public async Task<IActionResult> GetGender(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetGenderQuery(), cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }

    [CustomAuthorize]
    [HttpGet("regions")]
    public async Task<IActionResult> GetRegions(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRegionsQuery(), cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }

    [CustomAuthorize]
    [HttpGet("club-types")]
    public async Task<IActionResult> GetClubTypes(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetClubTypesQuery(), cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }

}
