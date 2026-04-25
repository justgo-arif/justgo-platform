using Asp.Versioning;
using JustGo.Authentication.Infrastructure.AbacAuthorization;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Application.Features.MemberFamily.Commands.AddFamilyMember;
using JustGo.MemberProfile.Application.Features.MemberFamily.Commands.DeleteFamilyMember;
using JustGo.MemberProfile.Application.Features.MemberFamily.Commands.FacilityRequestAction;
using JustGo.MemberProfile.Application.Features.MemberFamily.Commands.UpdateMemberFamilyName;
using JustGo.MemberProfile.Application.Features.MemberFamily.Commands.FamilyUpdateManager;
using JustGo.MemberProfile.Application.Features.MemberFamily.Queries.GetFamilyJoinRequest;
using JustGo.MemberProfile.Application.Features.MemberFamily.Queries.GetFamilyMemberMemberships;
using JustGo.MemberProfile.Application.Features.MemberFamily.Queries.GetFamilyMembers;
using JustGo.MemberProfile.Application.Features.MemberFamily.Queries.GetFamilyRequestDetails;
using JustGo.MemberProfile.Application.Features.MemberFamily.Queries.GetFamilySummary;
using JustGo.MemberProfile.Application.Features.Members.Queries.SearchMembersForFamily;
using JustGo.MemberProfile.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.MemberProfile.API.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/member-families")]
[ApiController]
[Tags("Member Profile/Member Family Details")]
public class MemberFamilyController : ControllerBase
{
    readonly IMediator _mediator;
    private readonly ICustomError _error;
    private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;

    public MemberFamilyController(IMediator mediator, ICustomError error, IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
    {
        _mediator = mediator;
        _error = error;
        _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
    }

    [CustomAuthorize]
    [ProducesResponseType(typeof(ApiResponse<Family, object>), StatusCodes.Status200OK)]
    [HttpGet("family-summary/{id:guid:required}")]
    //[ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id,Authorization")]
    public async Task<IActionResult> GetFamilySummary([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var resource = new Dictionary<string, object>()
            {{"id", id.ToString().ToLower()}};

        var permissions = await _abacPolicyEvaluatorService.EvaluatePolicyMultiAsync("family_allow_ui", cancellationToken, "ui", resource);

        Family? data = null;
        var joinRequestResult = await _mediator.Send(new GetFamilyJoinRequestQuery(id), cancellationToken);
        if (joinRequestResult.Count > 0)
        {
            return Ok(new ApiResponse<object, object>(data, permissions, 200, "Member have pending family join request."));
        }

        var result = await _mediator.Send(new GetFamilySummaryQuery(id), cancellationToken);
        if (result is null)
        {
            return Ok(new ApiResponse<object, object>(result, permissions, 200, "Member have no family."));
        }

        return Ok(new ApiResponse<Family, object>(result, permissions));
    }

    [CustomAuthorize]
    [ProducesResponseType(typeof(ApiResponse<List<FamilyMember>, object>), StatusCodes.Status200OK)]
    [HttpGet("family-members/{id:guid:required}")]
    //[ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id,Authorization")]
    public async Task<IActionResult> GetFamilyMembers([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var resource = new Dictionary<string, object>()
            {{"id", id.ToString().ToLower()}};

        var permissions = await _abacPolicyEvaluatorService.EvaluatePolicyMultiAsync("family_allow_ui", cancellationToken, "ui", resource);

        var joinRequestResult = await _mediator.Send(new GetFamilyJoinRequestQuery(id), cancellationToken);
        if (joinRequestResult.Count > 0)
        {
            return Ok(new ApiResponse<object, object>(new List<object>(), permissions, 200, "Member have pending family join request."));
        }

        var result = await _mediator.Send(new GetFamilyMembersQuery(id), cancellationToken);
        if (result.Count == 0)
        {
            return Ok(new ApiResponse<object, object>(result, permissions, 200, "No members found"));
        }
        return Ok(new ApiResponse<List<FamilyMember>, object>(result, permissions));
    }

    [CustomAuthorize]
    [HttpGet("search-member")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id,Authorization")]
    [ProducesResponseType(typeof(ApiResponse<List<FindMemberDto>, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchMemberToAddFamily([FromQuery] SearchMembersForFamilyQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        if (result.Count == 0)
        {
            return Ok(new ApiResponse<object, object>(result, 200, "No member found"));
        }
        return Ok(new ApiResponse<List<FindMemberDto>, object>(result));
    }

    [CustomAuthorize]
    [HttpPatch("update-family-name")]
    public async Task<IActionResult> Update([FromBody] UpdateMemberFamilyNameCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }

    [CustomAuthorize]
    [HttpGet("family-member-memberships/{guid:guid:required}")]
    public async Task<IActionResult> GetFamilyMemberMemberships([FromRoute] Guid guid, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFamilyMemberMembershipsQuery(guid), cancellationToken);
        if (result is null)
        {
            _error.NotFound<object>("Member have no Membership.");
            return new EmptyResult();
        }
        return Ok(new ApiResponse<List<OrganisationType>, object>(result));
    }

    [CustomAuthorize]
    [HttpDelete("delete-family-member/{userFamilyId:int}")]
    public async Task<IActionResult> DeleteFamilyMember(int userFamilyId, CancellationToken cancellationToken)
    {
        var command = new DeleteFamilyMemberCommand(userFamilyId);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }


    [CustomAuthorize]
    [HttpPost("add-family-member")]
    public async Task<IActionResult> AddFamilyMember([FromBody] AddFamilyMemberCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }

    [CustomAuthorize]
    [ProducesResponseType(typeof(ApiResponse<List<FamilyJoinRequestDto>, object>), StatusCodes.Status200OK)]
    [HttpGet("family-request/{id:guid:required}")]
    public async Task<IActionResult> GetFamilyJoinRquests([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFamilyJoinRequestQuery(id), cancellationToken);
        if (result.Count == 0)
        {
            return Ok(new ApiResponse<object, object>(result, 200, "No requests found"));
        }
        return Ok(new ApiResponse<List<FamilyJoinRequestDto>, object>(result));
    }

    [CustomAuthorize]
    [ProducesResponseType(typeof(ApiResponse<List<FamilyRequestDetailsDto>, object>), StatusCodes.Status200OK)]
    [HttpGet("family-request-details/{recordId:guid:required}")]
    public async Task<IActionResult> GetFamilyJoinRequestDetails([FromRoute] Guid recordId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFamilyRequestDetailsQuery(recordId), cancellationToken);
        if (result.Count == 0)
        {
            return Ok(new ApiResponse<object, object>(result, 200, "No requests found"));
        }
        return Ok(new ApiResponse<List<FamilyRequestDetailsDto>, object>(result));
    }


    [CustomAuthorize]
    [HttpPatch("family-request-action")]
    public async Task<IActionResult> FamilyRequestAction([FromBody] FamilyRequestActionCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }


    [CustomAuthorize]
    [HttpPatch("family-update-manager")]
    public async Task<IActionResult> FamilyMakeManager([FromBody] FamilyUpdateManagerCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<object, object>(result));
    }


}
