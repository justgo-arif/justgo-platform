using Asp.Versioning;
using JustGo.Authentication.Helper.Attributes;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.FieldManagement.Domain.Entities;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Application.Features.Members.Commands.AddFamilyMember;
using JustGo.MemberProfile.Application.Features.Members.Commands.DeleteFamilyMember;
using JustGo.MemberProfile.Application.Features.Members.Commands.GenerateFamilyActionToken;
using JustGo.MemberProfile.Application.Features.Members.Commands.SendVerificationMail;
using JustGo.MemberProfile.Application.Features.Members.Commands.SetMemberPhoto;
using JustGo.MemberProfile.Application.Features.Members.Commands.UpdateMember;
using JustGo.MemberProfile.Application.Features.Members.Queries.ExtentionSelectedData;
using JustGo.MemberProfile.Application.Features.Members.Queries.FamilyActionToken;
using JustGo.MemberProfile.Application.Features.Members.Queries.FindMember;
using JustGo.MemberProfile.Application.Features.Members.Queries.GetAllMembers;
using JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberBasicInfoBySyncGuid;
using JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberSummaryBySyncGuid;
using JustGo.MemberProfile.Application.Features.Members.Queries.MemberDetailsMenu;
using JustGo.MemberProfile.Application.Features.Preferences.Commands.SaveUserPreference;
using JustGo.MemberProfile.Application.Features.Preferences.Queries.GetUserPreferences;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.MemberProfile.API.Controllers.V1
{
    [ApiVersion("1.0")]
    //[ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/members")]
    [ApiController]
    [Tags("Member Profile/Members")]
    public class MembersController : ControllerBase
    {
        IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;
        private readonly ICustomError _error;
        public MembersController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService, ICustomError error)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
            _error = error;
        }

        [CustomAuthorize]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllMembers(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllMembersQuery(), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        //[CustomAuthorize("member_view_detail", "view", "id")]
        //[ProducesResponseType(typeof(ApiResponse<MemberSummeryDto, object>), StatusCodes.Status200OK)]
        //[HttpGet("summary/{id:guid}")]
        //public async Task<IActionResult> GetMemberSummaryBySyncGuid(Guid id, CancellationToken cancellationToken)
        //{
        //    var result = await _mediator.Send(new GetMemberSummaryBySyncGuidQuery(id), cancellationToken);
        //    if (result is null)
        //    {
        //        _error.NotFound<object>("Record not found");
        //        return new EmptyResult();
        //    }
        //    return Ok(new ApiResponse<object, object>(result));
        //}

        //[CustomAuthorize("member_view_detail", "view", "id")]
        //[HttpGet("basic-info/{id}")]
        //public async Task<IActionResult> GetMemberBasicInfoBySyncGuid(string id, CancellationToken cancellationToken)
        //{
        //    var result = await _mediator.Send(new GetMemberBasicInfoBySyncGuidQuery(new Guid(id)), cancellationToken);
        //    //var permissions1 = await _abacPolicyEvaluatorService.EvaluatePolicyMultiAsync("member_fields");
        //    var permissions = await _abacPolicyEvaluatorService.GetFieldPermissions(result, "member_field", null, cancellationToken);
        //    return Ok(new ApiResponse<object, object>(result, permissions));
        //}

        //[CustomAuthorize("member_edit", "edit")]
        //[HttpPut("basic-info/{id}")]
        //public async Task<IActionResult> UpdateBasicInfo(string id, UpdateMemberCommand command, CancellationToken cancellationToken)
        //{
        //    command.UserSyncId = new Guid(id);
        //    var existingRecord = await _mediator.Send(new GetMemberBasicInfoBySyncGuidQuery(new Guid(id)), cancellationToken);
        //    //var resource = new { };
        //    var modifiedFields = _abacPolicyEvaluatorService.GetModifiedFields(command, existingRecord);
        //    foreach (var field in modifiedFields)
        //    {
        //        var isPermitted = true;
        //        var policyName = $"member_field_{field}";
        //        //var isPermitted = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, "edit", resource);

        //        var permission = await _abacPolicyEvaluatorService.EvaluatePolicyMultiAsync(policyName, cancellationToken);
        //        if (permission is not null && permission.TryGetValue(field, out var fieldPermission))
        //        {
        //            isPermitted = fieldPermission.Edit;
        //        }
        //        if (!isPermitted)
        //        {
        //            _error.CustomValidation<object>($"You are not authorized to edit {field}");
        //            return new EmptyResult();
        //            //throw new ForbiddenAccessException($"You are not authorized to edit {field}");
        //        }
        //    }

        //    var result = await _mediator.Send(command, cancellationToken);
        //    return Ok(new ApiResponse<object, object>(result));
        //}

        [CustomAuthorize]
        [HttpGet("find-member")]
        [ProducesResponseType(typeof(ApiResponse<FindMemberDto, object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> FindMember([FromQuery] FindMemberQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            if (result is null)
            {
                _error.NotFound<object>("No member found.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<FindMemberDto, object>(result));
        }


        //[CustomAuthorize]
        //[HttpPost("add-family-member")]
        //public async Task<IActionResult> AddFamilyMember([FromBody] AddFamilyMemberCommand command, CancellationToken cancellationToken)
        //{
        //    var result = await _mediator.Send(command, cancellationToken);
        //    return Ok(new ApiResponse<object, object>(new { FamilyDocId = result }));
        //}

        //[CustomAuthorize]
        //[HttpPost("send-verification-mail")]
        //public async Task<IActionResult> SendVerificationMail([FromBody] SendVerificationMailCommand command, CancellationToken cancellationToken)
        //{
        //    var result = await _mediator.Send(command, cancellationToken);
        //    return Ok(new ApiResponse<object, object>(result));
        //}

        [CustomAuthorize("allowMemberProfileView", "view")]
        [ProducesResponseType(typeof(ApiResponse<EntityExtensionUI, object>), StatusCodes.Status200OK)]
        [HttpGet("details-menu")]
        public async Task<IActionResult> GetMemberDetailsMenu([FromQuery] GetMemberDetailsMenuQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        //[CustomAuthorize("allowMemberProfileView", "view")]
        //[HttpGet("selected-data/{exId}/{docId}")]
        //public async Task<IActionResult> SelectedData(int exId, int docId, CancellationToken cancellationToken)
        //{
        //    var result = await _mediator.Send(new GetExtentionSelectedDataQuery(exId, docId), cancellationToken);
        //    return Ok(new ApiResponse<object, object>(result));
        //}

        //[CustomAuthorize]
        //[HttpDelete("{familyDo//}cId:int}/{memberDocId:int}")]
        //public async Task<IActionResult> DeleteFamilyMember(int familyDocId, int memberDocId, CancellationToken cancellationToken)
        //{
        //    var command = new DeleteFamilyMemberCommand(familyDocId, memberDocId);
        //    var result = await _mediator.Send(command, cancellationToken);
        //    return Ok(new ApiResponse<object, object>(result));
        

        // Adds an existing member to a family.
        // The user must call this endpoint, then check their email to confirm the action.
        [HttpPost("generate-token")]
        public async Task<IActionResult> GenerateFamilyActionToken([FromBody] GenerateFamilyActionTokenCommand request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<int, object>(result)); // Adjust ApiResponse format as needed
        }

        [AllowAnonymous]
        [TenantFromHeader]
        [HttpGet("family-action-token/invoke")]
        public async Task<IActionResult> FamilyActionToken([FromQuery] FamilyActionTokenQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);

            if (result.Success && !string.IsNullOrEmpty(result.RedirectUrl))
            {
                return Redirect(result.RedirectUrl);
            }
            return Ok(new ApiResponse<object, object>(result));
        }

        [AllowAnonymous]
        [HttpGet("family-link-feedback")]
        public IActionResult FamilyLinkFeedback([FromQuery] string message = "Thanks, you have been successfully linked to your family")
        {
            return Ok(new ApiResponse<object, object>(message));
        }

        //[CustomAuthorize]
        //[ProducesResponseType(typeof(ApiResponse<bool, object>), StatusCodes.Status200OK)]
        //[HttpPost("set-photo")]
        //public async Task<IActionResult> SetMemberPhoto([FromBody] SetMemberPhotoCommand command)
        //{
        //    var result = await _mediator.Send(command);
        //    if (!result)
        //        return BadRequest("Failed to set user photo.");
        //    return Ok(new ApiResponse<object, object>(result));
        //}

        [CustomAuthorize]
        [MapToApiVersion("1.0")]
        [HttpPost("save-preference")]
        public async Task<IActionResult> SaveUserPreference([FromBody] SaveUserPreferenceCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        //[MapToApiVersion("1.0")]
        [HttpGet("user-preference")]
        public async Task<IActionResult> GetUserPreferences([FromQuery] string memberDocId, int organizationId, int preferenceTypeId, CancellationToken cancellationToken)
        {

            var result = await _mediator.Send(new GetUserPreferencesQuery(memberDocId, organizationId, preferenceTypeId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
    }
}
