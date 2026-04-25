using Asp.Versioning;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Credential.Application.DTOs;
using JustGo.Credential.Application.Features.Credentials.Queries.GetCredentialsCategories;
using JustGo.Credential.Application.Features.Credentials.Queries.GetCredentialsMetaData;
using JustGo.Credential.Application.Features.Credentials.Queries.GetCredentialSummary;
using JustGo.Credential.Application.Features.Credentials.Queries.GetMemberCredentials;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.Credential.API.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/credentials")]
[ApiController]
[Tags("Profile Credentials/Credentials")]
public class CredentialsController : ControllerBase
{
    readonly IMediator _mediator;
    private readonly ICustomError _error;
    public CredentialsController(IMediator mediator, ICustomError error)
    {
        _mediator = mediator;
        _error = error;
    }

    [CustomAuthorize]
    [HttpGet("credential-summary/{userGuid:guid:required}")]
    [ProducesResponseType(typeof(ApiResponse<CredentialSummaryDto, object>), StatusCodes.Status200OK)]
    //[ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client, VaryByHeader = "Authorization")]
    public async Task<IActionResult> GetCredentialSummary([FromRoute] Guid userGuid, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCredentialSummaryQuery(userGuid), cancellationToken);
        return Ok(new ApiResponse<CredentialSummaryDto, object>(result));
    }

    [CustomAuthorize]
    [HttpPost("member-credentials")]
    [ProducesResponseType(typeof(ApiResponse<List<CredentialsDto>, object>), StatusCodes.Status200OK)]
    //[ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client, VaryByHeader = "Authorization")]
    public async Task<IActionResult> GetMemberCredentials([FromBody] GetMemberCredentialsQuery request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        if (!result.IsSuccess)
        {
            _error.CustomValidation<object>(result.Message);
            return new EmptyResult();
        }
        return Ok(new ApiResponse<List<CredentialsDto>, object>(result.Data));
    }

    [CustomAuthorize]
    [ProducesResponseType(typeof(ApiResponse<List<CredentialsCategoriesDto>, object>), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client, VaryByHeader = "Authorization")]
    [HttpGet("credentials-categories")]
    public async Task<IActionResult> GetCredentialsCategories(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCredentialsCategoriesQuery(), cancellationToken);
        return Ok(new ApiResponse<List<CredentialsCategoriesDto>, object>(result));
    }

    [CustomAuthorize]
    [ProducesResponseType(typeof(ApiResponse<FilterMetaDataDTO, object>), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client, VaryByHeader = "Authorization")]
    [HttpGet("credentials-metadata/{Id:guid:required}")]
    public async Task<IActionResult> GetCredentialsMetaData([FromRoute] Guid Id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCredentialsMetaDataQuery(Id), cancellationToken);
        if (!result.IsSuccess)
        {
            _error.CustomValidation<object>(result.Message);
            return new EmptyResult();
        }
        return Ok(new ApiResponse<FilterMetaDataDTO, object>(result.Data));
    }
}
