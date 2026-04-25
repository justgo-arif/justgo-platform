using Asp.Versioning;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.Membership.Application.Features.Memberships.Queries.GetMemberMemberships;
using JustGo.Membership.Application.Features.Memberships.Queries.GetMembershipDownloadLinks;
using JustGo.Membership.Application.Features.Memberships.Queries.GetMembershipsBySyncGuid;
using JustGo.Membership.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.Membership.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/memberships")]
    [ApiController]
    [Tags("Membership/Memberships")]
    public class MembershipsController : ControllerBase
    {
        IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;

        public MembershipsController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
        }


        [CustomAuthorize("allowMembershipView", "view", "guid")]
        [MapToApiVersion("1.0")]
        [HttpGet("memberships/{guid}")]
        public async Task<IActionResult> GetMembershipsByMemberSyncGuid(string guid, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMembershipsBySyncGuidQuery(new Guid(guid)), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        [CustomAuthorize("allowMembershipCreate", "create")]
        [MapToApiVersion("1.0")]
        [HttpPost("create")]
        public async Task<IActionResult> Create()
        {
            return Ok("Success");
        }
        [CustomAuthorize("allowMembershipUpdate", "update")]
        [MapToApiVersion("1.0")]
        [HttpPut("update")]
        public async Task<IActionResult> Update(object model)
        {
            var modifiedFields = GetModifiedFields(model);
            foreach (var field in modifiedFields)
            {
                var isEditable = true;//await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, input);

                if (!isEditable)
                {
                    return Forbid($"You do not have permission to edit {field}");
                }
            }
            return Ok("Success");
        }
        private List<string> GetModifiedFields(object model)
        {
            var modifiedFields = new List<string>();
            return modifiedFields;
        }
        [CustomAuthorize("allowMembershipDelete", "delete")]
        [MapToApiVersion("1.0")]
        [HttpDelete("delete")]
        public async Task<IActionResult> Delete()
        {
            return Ok("Success");
        }
        [CustomAuthorize("allowRulesUpdate", "update")]
        [MapToApiVersion("1.0")]
        [HttpPatch("rules/update")]
        public async Task<IActionResult> UpdateRules()
        {
            return Ok("Success");
        }


        [CustomAuthorize("membership_allow_view","view","id")]
        [MapToApiVersion("1.0")]
        [HttpGet("member-memberships/{id}")]
        [ProducesResponseType(typeof(ApiResponse<List<MembersHierarchiesWithMemberships>, object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMemberMemberships(string id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMemberMembershipsQuery(new Guid(id)), cancellationToken);
            var resource = new Dictionary<string, object>()
            {{"id", id.ToLower()}};

            var permissions = await _abacPolicyEvaluatorService.EvaluatePolicyMultiAsync("membership_allow_ui", cancellationToken, "ui", resource);
            return Ok(new ApiResponse<object, object>(result, permissions));
        }

        [CustomAuthorize]
        [MapToApiVersion("1.0")]
        [HttpGet("memberships-downloadlinks/{guid}")]
        [ProducesResponseType(typeof(ApiResponse<MembershipDownloadLinks, object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMembershipDownloadLinks(string guid, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMembershipDownloadLinksQuery(new Guid(guid)), cancellationToken);

            return Ok(new ApiResponse<object, object>(result));
        }

    }
}
