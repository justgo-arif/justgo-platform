using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asp.Versioning;
using JustGo.AssetManagement.Application.Features.AssetLeases.Commands.CreateLeases;
using JustGo.AssetManagement.Application.Features.AssetLeases.Queries.GetLeaseActivityLog;
using JustGo.AssetManagement.Application.Features.AssetLeases.Queries.GetLeaseById;
using JustGo.AssetManagement.Application.Features.AssetLeases.Queries.GetLeaseHistory;
using JustGo.AssetManagement.Application.Features.AssetLeases.Queries.GetMyLeases;
using JustGo.AssetManagement.Application.Features.AssetLeases.Queries.LeaseAdditionalFees;
using JustGo.AssetManagement.Application.Features.AssetLeases.Queries.OwnerLeaseApprovalMetadata;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetStatusCommands;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.RegisterAssets;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.AssetManagement.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/asset-leases")]
    [ApiController]
    [Tags("Asset Management/Asset Leases")]
    public class AssetLeasesController : ControllerBase
    {
        IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;
        private readonly ICustomError _error;
        public AssetLeasesController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService, ICustomError error)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
            _error = error;
        }

        [CustomAuthorize("asset_create_lease", "create", "assetRegisterId")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateAssetLease([FromBody] CreateAssetLeaseCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            if( result == null)
            {
                _error.Conflict<object>("Lease already exists for this asset during the specified period.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("asset_edit_lease", "edit")]
        [HttpPut("edit/{assetLeaseId}")]
        public async Task<IActionResult> EditAssetLease(string assetLeaseId, [FromBody] EditAssetLeaseCommand command, CancellationToken cancellationToken)
        {
            command.AssetLeaseId = assetLeaseId;
            var result = await _mediator.Send(command, cancellationToken);
            if (result == null)
            {
                _error.Conflict<object>("Lease already exists for this asset during the specified period.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpPost("my-leases")]
        public async Task<IActionResult> MyLeases([FromBody] GetMyLeasesQuery request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }


        [CustomAuthorize]
        [HttpPost("lease-activity-log/{assetLeaseId}")]
        public async Task<IActionResult> GetLeaseActivityLog(string assetLeaseId, [FromBody] GetLeaseActivityLogQuery request, CancellationToken cancellationToken)
        {
            request.AssetLeaseId = assetLeaseId;
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpPost("lease-history")]
        public async Task<IActionResult> LeaseHistory([FromBody] GetLeaseHistoryQuery request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }
        [CustomAuthorize]
        [HttpGet("asset-lease-ui-permissions/{assetRegisterId}")]
        public async Task<IActionResult> GetAssetLeasePermissions(string assetRegisterId, CancellationToken cancellationToken)
        {
            var resource = new Dictionary<string, object>()
            {
                { "assetRegisterId", assetRegisterId }
            };
            var permissions = await _abacPolicyEvaluatorService.EvaluatePolicyAsync("asset_allow_ui_lease", cancellationToken, null, resource);
            return Ok(new ApiResponse<object, object>(null, permissions));
        }

        [CustomAuthorize]
        [HttpPatch("change-status/{assetLeaseId}")]
        public async Task<IActionResult> ChangeAssetLeaseStatus(string assetLeaseId, [FromBody] ChangeAssetLeaseStatusCommand command, CancellationToken cancellationToken)
        {
            command.AssetLeaseId = assetLeaseId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize("asset_view_lease", "view")]
        [HttpGet("details/{assetLeaseId}")]
        public async Task<IActionResult> GetSingleLease(string assetLeaseId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAssetLeaseByIdQuery() { AssetLeaseId = assetLeaseId}, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpGet("additional-fee/{assetLeaseId}")]
        public async Task<IActionResult> AdditionalFee(string assetLeaseId, string ownerId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetLeaseAdditionalFeeQuery(assetLeaseId, ownerId), cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpGet("lease-owner-approval-metadata/{assetLeaseId}")]
        public async Task<IActionResult> LeaseOwnerApproval(string assetLeaseId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetOwnerLeaseApprovalMetadataQuery()
            {
                LeaseId = assetLeaseId 
            }, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

    }
}
