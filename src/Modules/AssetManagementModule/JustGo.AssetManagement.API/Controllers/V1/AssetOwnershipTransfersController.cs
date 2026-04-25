using Asp.Versioning;
using JustGo.AssetManagement.Application.Features.AssetTransfers.Queries.GetTransferById;
using JustGo.AssetManagement.Application.Features.AssetOwnershipTransfers.Commands.CreateAssetTransfers;
using JustGo.AssetManagement.Application.Features.AssetOwnershipTransfers.Queries.GetTransferHistory;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using JustGo.AssetManagement.Application.Features.AssetTransfers.Queries.OwnerTransferApprovalMetadata;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetStatusCommands;
using JustGo.AssetManagement.Application.Features.AssetOwnershipTransfers.Queries.GetTransferActivityLog;

namespace JustGo.AssetManagement.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/asset-ownership-transfer")]
    [ApiController]
    [Tags("Asset Management/Asset Ownership Transfer")]
    public class AssetOwnershipTransfersController : ControllerBase
    {
        IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;
        private readonly ICustomError _error;
        public AssetOwnershipTransfersController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService, ICustomError error)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
            _error = error;
        }

        [CustomAuthorize("asset_create_transfer", "create", "assetRegisterId")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateAssetTransfer([FromBody] CreateAssetTransferCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }


        [CustomAuthorize]
        [HttpPost("ownership-history")]
        public async Task<IActionResult> TransferHistory([FromBody] GetTransferHistoryQuery request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpGet("asset-transfer-ui-permissions/{assetRegisterId}")]
        public async Task<IActionResult> GetAssetLeasePermissions(string assetRegisterId, CancellationToken cancellationToken)
        {
            var resource = new Dictionary<string, object>()
            {
                { "assetRegisterId", assetRegisterId }
            };
            var permissions = await _abacPolicyEvaluatorService.EvaluatePolicyAsync("asset_allow_ui_transfer", cancellationToken, null, resource);
            return Ok(new ApiResponse<object, object>(null, permissions));
        }

        [CustomAuthorize("asset_view_transfer", "view")]
        [HttpGet("details/{assetTransferId}")]
        public async Task<IActionResult> GetSingleTransfer(string assetTransferId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAssetTransferByIdQuery() { AssetTransferId = assetTransferId }, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpPost("transfer-activity-log/{assetTransferId}")]
        public async Task<IActionResult> GetTransferActivityLog(string assetTransferId, [FromBody] GetTransferActivityLogQuery request, CancellationToken cancellationToken)
        {
            request.AssetTransferId = assetTransferId;
            var result = await _mediator.Send(request, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpPatch("change-status/{assetTransferId}")]
        public async Task<IActionResult> ChangeAssetTransferStatus(string assetTransferId, [FromBody] ChangeAssetTransferStatusCommand command, CancellationToken cancellationToken)
        {
            command.AssetTransferId = assetTransferId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

        [CustomAuthorize]
        [HttpGet("transfer-owner-approval-metadata/{assetTransferId}")]
        public async Task<IActionResult> TransferOwnerApproval(string assetTransferId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetOwnerTransferApprovalMetadataQuery()
            {
                TransferId = assetTransferId
            }, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }


    }
}
