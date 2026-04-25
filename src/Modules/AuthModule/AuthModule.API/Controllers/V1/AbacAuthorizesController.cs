using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asp.Versioning;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthModule.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/abac-authorizes")]
    [ApiController]
    [Tags("Authentication/AbacAuthorizes")]
    public class AbacAuthorizesController : ControllerBase
    {
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;

        public AbacAuthorizesController(IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
        {
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
        }

        [CustomAuthorize]
        [HttpPost("evaluate-policy/{policyName}/{actionAttribute}")]
        public async Task<IActionResult> EvaluatePolicyAsync(string policyName, string actionAttribute, [FromBody] Dictionary<string, object> resource, CancellationToken cancellationToken = default)
        {
            var result = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, actionAttribute, resource.Any() ? resource : null, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        [CustomAuthorize]
        [HttpGet("{policyName}")]
        public async Task<IActionResult> GetUiPermissions(string policyName, CancellationToken cancellationToken)
        {
            var result = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        [CustomAuthorize]
        [HttpPost("{policyName}/{actionAttribute}")]
        public async Task<IActionResult> GetUiPermissions(string policyName, string actionAttribute, [FromBody] Dictionary<string, object> resource, CancellationToken cancellationToken)
        {
            var result = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, cancellationToken, actionAttribute, resource.Any() ? resource : null);
            return Ok(new ApiResponse<object, object>(result));
        }
        [CustomAuthorize]
        [HttpGet("fields/{policyName}")]
        public async Task<IActionResult> GetFieldPermissions(string policyName, CancellationToken cancellationToken)
        {
            var result = await _abacPolicyEvaluatorService.EvaluatePolicyMultiAsync(policyName, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

    }
}
