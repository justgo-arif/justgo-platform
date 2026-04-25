using Asp.Versioning;
using JustGo.Authentication.Infrastructure.AbacAuthorization;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberEmergencyContactBySyncGuid;
using JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Commands.CreateUserEmergencyContacts;
using JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Commands.DeleteUserEmergencyContacts;
using JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Commands.SetPrimaryUserEmergencyContact;
using JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Commands.UpdateUserEmergencyContacts;
using JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Queries.GetEmergencyContactMandatorySettings;
using JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Queries.GetRelationship;
using JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Queries.GetOwnerUserbyContactGuid;
using JustGo.MemberProfile.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.MemberProfile.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/emergency-contacts")]
    [ApiController]
    [Tags("Member Profile/User Emergency Contacts")]
    public class UserEmergencyContactsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;

        public UserEmergencyContactsController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
        }

        [CustomAuthorize("emergency_contacts_view", "view","id")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserEmergencyContact>, object>), StatusCodes.Status200OK)]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetMemberEmergencyContactBySyncGuid(Guid id, CancellationToken cancellationToken)
         {
            var contacts = await _mediator.Send(new GetMemberEmergencyContactBySyncGuidQuery(id), cancellationToken);
            var meta = await _mediator.Send(new GetEmergencyContactMandatorySettingsQuery(), cancellationToken);

            var resource = new Dictionary<string, object>(){{ "id", id.ToString().ToLower() } };

            var permissions = await _abacPolicyEvaluatorService.EvaluatePolicyMultiAsync
                ("user_emergencycontact_allow_ui", cancellationToken, "ui", resource);

            dynamic perms = permissions;

            var merged = meta.GetType()
                .GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(meta));

            merged["emergency_contacts"] = perms["emergency_contacts"];

            return Ok(new ApiResponse<object, object>(contacts, merged));
        }

        [CustomAuthorize]
        [ProducesResponseType(typeof(ApiResponse<int, object>), StatusCodes.Status200OK)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserEmergencyContactCommand command, CancellationToken cancellationToken)
        {
            var policyName = "emergency_contact_manage";
            var resource = new Dictionary<string, object>() { { "id", command.UserSyncGuid.ToString().ToLower() } };

            var isPermitted = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, "add", resource, cancellationToken);
            if (!isPermitted)
            {
                throw new ForbiddenAccessException();
            }
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [ProducesResponseType(typeof(ApiResponse<int, object>), StatusCodes.Status200OK)]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateUserEmergencyContactCommand command, CancellationToken cancellationToken)
        {
            var userBasicInfo = await _mediator.Send(new GetOwnerUserbyContactGuidQuery(Guid.Parse(command.SyncGuid)), cancellationToken);
            var policyName = "emergency_contact_manage";
            var resource = new Dictionary<string, object>() { { "id", userBasicInfo.Id.ToString().ToLower() } };

            var isPermitted = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, "edit", resource, cancellationToken);
            if (!isPermitted)
            {
                throw new ForbiddenAccessException();
            }

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [ProducesResponseType(typeof(ApiResponse<int, object>), StatusCodes.Status200OK)]
        [HttpPatch("{id:int}/set-primary")]
        public async Task<IActionResult> SetPrimary(int id, CancellationToken cancellationToken)
        {
            var command = new SetPrimaryUserEmergencyContactCommand(id);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [ProducesResponseType(typeof(ApiResponse<int, object>), StatusCodes.Status200OK)]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var userBasicInfo = await _mediator.Send(new GetOwnerUserbyContactGuidQuery(id), cancellationToken);
            var policyName = "emergency_contact_manage";
            var resource = new Dictionary<string, object>(){{"id", userBasicInfo.Id.ToString().ToLower()}};

            var isPermitted = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, "delete", resource, cancellationToken);
            if (!isPermitted)
            {
                throw new ForbiddenAccessException();
            }

            var command = new DeleteUserEmergencyContactCommand(id);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [ProducesResponseType(typeof(ApiResponse<List<UserRelationshipDto>, object>), StatusCodes.Status200OK)]
        [HttpGet("relationships")]
        public async Task<IActionResult> GetRelationships(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetRelationshipQuery(), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

    }
}
