using Asp.Versioning;
using JustGo.Authentication.Infrastructure.AbacAuthorization;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Application.Features.Preferences.Commands.SaveCurrentPreference;
using JustGo.MemberProfile.Application.Features.Preferences.Commands.SaveOptinCurrent;
using JustGo.MemberProfile.Application.Features.Preferences.GetCurrentPreferencesBySyncGuid;
using JustGo.MemberProfile.Application.Features.Preferences.GetOptInCurrentsBySyncGuid;
using JustGo.MemberProfile.Application.Features.Preferences.GetOptInMasterByOwner;
using JustGo.MemberProfile.Application.Features.Preferences.GetOptInsWithCurrent;
using JustGo.MemberProfile.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace JustGo.MemberProfile.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/preferences")]
    [ApiController]
    //[ApiModule("Member Profile")]
    [Tags("Opt In/Preferences")]
    public class PreferencesController : ControllerBase
    {
        IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;

        public PreferencesController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
        }

        [CustomAuthorize]
        [MapToApiVersion("1.0")]
        [HttpGet("current-optins/{guid}")]
        public async Task<IActionResult> GetOptInCurrentsBySyncGuid(string guid, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetOptInCurrentsBySyncGuidQuery(new Guid(guid)), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }


        [CustomAuthorize]
        [MapToApiVersion("1.0")]
        [HttpGet("optin-master/{guid}/{ownerType}/{ownerId}")]
        public async Task<IActionResult> GetOptInMasterByOwner(string guid, string ownerType, int ownerId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetOptInMasterByOwnerQuery(new Guid(guid), ownerType, ownerId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }


        [CustomAuthorize]
        [ProducesResponseType(typeof(ApiResponse<OptInMaster, object>), StatusCodes.Status200OK)]
        [MapToApiVersion("1.0")]
        [HttpGet("optins-with-current/{guid}")]
        public async Task<IActionResult> GetOptInsWithCurrent(string guid,CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetOptInsWithCurrentQuery(new Guid(guid)), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }


        [CustomAuthorize]
        [MapToApiVersion("1.0")]
        [HttpPost("save-optin-current")]
        public async Task<IActionResult> SaveOptinCurrent([FromBody] SaveOptinCurrentCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [ProducesResponseType(typeof(ApiResponse<CurrentPreference, object>), StatusCodes.Status200OK)]
        [MapToApiVersion("1.0")]
        [HttpGet("current-preference/{guid}")]
        public async Task<IActionResult> GetCurrentPreferencesBySyncGuid(string guid, CancellationToken cancellationToken)
        {
            var policyName = "user_preference_allow";
            var resource = new Dictionary<string, object>() { { "id", guid.ToString().ToLower() } };

            var isPermitted = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, "view", resource, cancellationToken);
            if (!isPermitted)
            {
                throw new ForbiddenAccessException();
            }

            var result = await _mediator.Send(new GetCurrentPreferencesBySyncGuidQuery(new Guid(guid)), cancellationToken);
            var permissions = await _abacPolicyEvaluatorService.EvaluatePolicyMultiAsync
                ("user_preference_allow_ui", cancellationToken, "ui", resource);
            return Ok(new ApiResponse<CurrentPreference, object>(result, permissions));
        }

        [CustomAuthorize]
        [MapToApiVersion("1.0")]
        [HttpPost("save-current-preference")]
        public async Task<IActionResult> SaveCurrentPreference([FromBody] SaveCurrentPreferenceCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new ApiResponse<object, object>(result));
        }

    }
}
