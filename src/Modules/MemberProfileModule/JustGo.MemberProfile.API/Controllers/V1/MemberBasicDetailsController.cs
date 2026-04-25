using Asp.Versioning;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Application.Features.Members.Commands.SendVerificationMail;
using JustGo.MemberProfile.Application.Features.Members.Commands.SetMemberPhoto;
using JustGo.MemberProfile.Application.Features.Members.Commands.UpdateMember;
using JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberNotification;
using JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberSummaryBySyncGuid;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.MemberProfile.API.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/member-basic-details")]
[ApiController]
[Tags("Member Profile/Member Basic Details")]
public class MemberBasicDetailsController : ControllerBase
{
    IMediator _mediator;
    private readonly ICustomError _error;
    private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;
    private readonly IMapper _mapper;
    public MemberBasicDetailsController(IMediator mediator, ICustomError error
        , IAbacPolicyEvaluatorService abacPolicyEvaluatorService, IMapper mapper)
    {
        _mediator = mediator;
        _error = error;
        _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
        _mapper = mapper;
    }

    [CustomAuthorize("member_view_detail", "view", "id")]
    [ProducesResponseType(typeof(ApiResponse<MemberSummaryDto, object>), StatusCodes.Status200OK)]
    [HttpGet("summary/{id:guid:required}")]
    public async Task<IActionResult> GetMemberSummary([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMemberSummaryBySyncGuidQuery(id), cancellationToken);
        if (result is null)
        {
            _error.NotFound<object>("Member not found.");
            return new EmptyResult();
        }
        var resource = new Dictionary<string, object>()
            {
                { "id", id }
            };
        var permissions = await _abacPolicyEvaluatorService.EvaluatePolicyMultiAsync
            ("member_edit_basic_fields", cancellationToken, "edit", resource);
        return Ok(new ApiResponse<MemberSummaryDto, object>(result, permissions));
    }

    [CustomAuthorize("member_edit_profile_picture", "edit", "userSyncId")]
    [HttpPost("profile-photo")]
    [ProducesResponseType(typeof(ApiResponse<OperationResultDto<string>, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfilePhoto([FromForm] SetMemberPhotoCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return BadRequest(result.Message);
        return Ok(new ApiResponse<OperationResultDto<string>, object>(result));
    }

    [CustomAuthorize("member_edit_basic_detail", "edit", "userSyncId")]
    [ProducesResponseType(typeof(ApiResponse<OperationResultDto<MemberSummaryDto>, object>), StatusCodes.Status200OK)]
    [HttpPut("basic-details")]
    public async Task<IActionResult> UpdateBasicDetails([FromBody] UpdateMemberCommand command, CancellationToken cancellationToken)
    {
        var existingRecord = await _mediator.Send(new GetMemberSummaryBySyncGuidQuery(command.UserSyncId), cancellationToken);
        if (existingRecord is null)
        {
            _error.NotFound<object>("Member not found.");
            return new EmptyResult();
        }
        var mappedExistingRecord = _mapper.Map<UpdateMemberCommand>(existingRecord);
        var modifiedFields = _abacPolicyEvaluatorService.GetModifiedFields(command, mappedExistingRecord);
        var resource = new Dictionary<string, object>()
            {
                { "id", command.UserSyncId }
            };
        var permissions = await _abacPolicyEvaluatorService.EvaluatePolicyMultiAsync("member_edit_basic_fields", cancellationToken, "edit", resource);
        foreach (var field in modifiedFields)
        {
            var isPermitted = true;
            if (permissions is not null)
            {
                var fieldPermission = permissions.FirstOrDefault(p => p.Key.Equals(field, StringComparison.OrdinalIgnoreCase)).Value;
                if (fieldPermission is not null)
                {
                    isPermitted = fieldPermission.Edit;
                }
            }
            if (!isPermitted)
            {
                _error.CustomValidation<object>($"You are not authorized to edit {field}");
                return new EmptyResult();
            }
        }

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            _error.CustomValidation<object>(result.Message);
            return new EmptyResult();
        }
        return Ok(new ApiResponse<OperationResultDto<MemberSummaryDto>, object>(result, permissions));
    }

    [CustomAuthorize("member_verify_email", "verify", "userSyncId")]
    [ProducesResponseType(typeof(ApiResponse<OperationResultDto, object>), StatusCodes.Status200OK)]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] SendVerificationMailCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            _error.NotFound<object>(result.Message);
            return new EmptyResult();
        }
        return Ok(new ApiResponse<OperationResultDto, object>(result));
    }

    [CustomAuthorize]
    [ProducesResponseType(typeof(ApiResponse<List<UserNotificationDto>, object>), StatusCodes.Status200OK)]
    [HttpGet("notification/{id:guid:required}")]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Client, VaryByHeader = "X-Tenant-Id,Authorization")]
    public async Task<IActionResult> GetMemberNotification([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMemberNotificationBySyncGuidQuery(id), cancellationToken);
        return Ok(new ApiResponse<List<UserNotificationDto>, object>(result));
    }

}
