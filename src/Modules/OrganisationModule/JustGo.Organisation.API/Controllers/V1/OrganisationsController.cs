using Asp.Versioning;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Application.Features.MemberFamily.Queries.GetFamilyRequestDetails;
using JustGo.Organisation.Application.DTOs;
using JustGo.Organisation.Application.Features.Organizations.Commands.AddClub;
using JustGo.Organisation.Application.Features.Organizations.Commands.CancelTransfer;
using JustGo.Organisation.Application.Features.Organizations.Commands.ClubTransferRequest;
using JustGo.Organisation.Application.Features.Organizations.Commands.LeaveClub;
using JustGo.Organisation.Application.Features.Organizations.Commands.SetPrimaryClub;
using JustGo.Organisation.Application.Features.Organizations.Queries.GetClubDetails;
using JustGo.Organisation.Application.Features.Organizations.Queries.GetClubs;
using JustGo.Organisation.Application.Features.Organizations.Queries.GetMyOrganisations;
using JustGo.Organisation.Application.Features.Organizations.Queries.GetOrganizationHierarchyByMemberSyncGuid;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.Organisation.API.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/organisations")]
[ApiController]
[Tags("Organisation/Organisations")]
public class OrganisationsController : ControllerBase
{
    readonly IMediator _mediator;
    private readonly ICustomError _error;

    public OrganisationsController(IMediator mediator, ICustomError error)
    {
        _mediator = mediator;
        _error = error;
    }

    [CustomAuthorize]
    [HttpPost("clubs")]
    [ProducesResponseType(typeof(ApiResponse<KeysetPagedResult<ClubDto>, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClubs([FromBody] GetClubsQuery request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        return Ok(new ApiResponse<KeysetPagedResult<ClubDto>, object>(result));
    }

    [CustomAuthorize]
    [HttpGet("club-details/{clubGuid:guid:required}/{userGuid:guid:required}")]
    //[ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client, VaryByHeader = "Authorization")]
    [ProducesResponseType(typeof(ApiResponse<ClubDetailsDto, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClubDetails(Guid clubGuid, Guid userGuid, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetClubDetailsQuery(clubGuid, userGuid), cancellationToken);
        if (result is null)
        {
            _error.NotFound<object>("Club details not found.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<ClubDetailsDto, object>(result));
    }

    [CustomAuthorize]
    [HttpPost("join-club")]
    [ProducesResponseType(typeof(ApiResponse<OperationResultDto, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> JoinClubMember([FromBody] JoinClubCommand request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        return Ok(new ApiResponse<OperationResultDto, object>(result));
    }

    [CustomAuthorize]
    [HttpPost("club-transfer-request")]
    [ProducesResponseType(typeof(ApiResponse<OperationResultDto<int>, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClubTransferRequest([FromBody] ClubTransferRequestCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            _error.CustomValidation<object>(result.Message);
            return new EmptyResult();
        }
        return Ok(new ApiResponse<OperationResultDto<int>, object>(result));
    }

    [CustomAuthorize]
    [MapToApiVersion("1.0")]
    [HttpGet("hierarchy-types/{guid}")]
    public async Task<IActionResult> GetOrganizationHierarchyByMemberSyncGuid(string guid, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrganizationHierarchyByMemberSyncGuidQuery(new Guid(guid)), cancellationToken);
        return Ok(new ApiResponse<object, object>(result.Select(s => new
        {
            s.Id,
            Name = s.HierarchyTypeName
        })));
    }

    [CustomAuthorize]
    [HttpPost("leave-club")]
    public async Task<IActionResult> RejectClubMember([FromBody] LeaveClubCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result.Message);
        return Ok(new ApiResponse<OperationResultDto<string>, object>(result));
    }

    [CustomAuthorize]
    [HttpGet("my-organisations/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<MyOrganisationDto>, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrganisations(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyOrganisationsQuery(id), cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }

    [CustomAuthorize]
    [HttpPost("set-primary")]
    [ProducesResponseType(typeof(ApiResponse<OperationResultDto, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetPrimaryClub([FromBody] SetPrimaryClubCommand request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(result.Message);
        return Ok(new ApiResponse<OperationResultDto, object>(result));

    }

    [CustomAuthorize]
    [HttpPost("cancel-transfer/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OperationResultDto, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelTransfer([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelTransferCommand(id), cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Message);
        }

        return Ok(new ApiResponse<OperationResultDto, object>(result));
    }

    [CustomAuthorize]
    [HttpGet("primary-club-details/{userGuid:guid:required}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PrimaryClubDto>, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPrimaryClubDetails(Guid userGuid, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMemberPrimaryClubDetailsQuery{UserGuid=userGuid}, cancellationToken);
        if (result is null)
        {
            _error.NotFound<object>("Primary Club details not found.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<IEnumerable<PrimaryClubDto>, object>(result));
    }

}
