using Asp.Versioning;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthModule.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [ApiVersion("3.0")]
    [Route("api/v{version:apiVersion}/ui-permissions")]
    [ApiController]
    [Tags("Authentication/UiPermissions")]
    public class UiPermissionsController : ControllerBase
    {
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;

        public UiPermissionsController(IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
        {
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
        }
        [CustomAuthorize]
        [HttpGet("{policyName}")]
        public async Task<IActionResult> GetUiPermissions(string policyName, CancellationToken cancellationToken, [FromQuery] Guid? organizationId = null)
        {
            var result = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, cancellationToken);
            return Ok(new ApiResponse<object, object>(null, result));
        }

        [CustomAuthorize]
        [HttpGet("fields/{policyName}")]
        public async Task<IActionResult> GetFieldPermissions(string policyName, CancellationToken cancellationToken)
        {
            var result = await _abacPolicyEvaluatorService.EvaluatePolicyMultiAsync(policyName, cancellationToken);
            return Ok(new ApiResponse<object, object>(null, result));
        }

        [CustomAuthorize]
        [HttpPost("by-params/{policyName}")]
        public async Task<IActionResult> GetUiPermissionsByParams(string policyName, CancellationToken cancellationToken, [FromBody] Dictionary<string, object> resource = null)
        {
            var result = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, cancellationToken, null, resource);
            return Ok(new ApiResponse<object, object>(null, result));
        }
    }
}
