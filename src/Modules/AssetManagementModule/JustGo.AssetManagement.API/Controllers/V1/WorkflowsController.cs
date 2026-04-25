using Asp.Versioning;
using JustGo.AssetManagement.Application.DTOs.Credential;
using JustGo.AssetManagement.Application.Features.AssetCredentials.Queries.GetAssetGuidByCredentialGuid;
using JustGo.AssetManagement.Application.Features.AssetLeases.Queries.GetLeaseById;
using JustGo.AssetManagement.Application.Features.AssetTransfers.Queries.GetTransferById;
using JustGo.AssetManagement.Application.Features.Workflows.Commands.WorkflowSubmissions;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;


namespace JustGo.AssetManagement.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/workflows")]
    [ApiController]
    [Tags("Asset Management/Workflows")]
    public class WorkflowsController:ControllerBase
    {
        IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;

        public WorkflowsController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
        }

        [CustomAuthorize]
        [HttpPost("submission")]
        public async Task<IActionResult> GetWorkflows(WorkflowSubmissionCommand command, CancellationToken cancellationToken)
        {
            var policyName = "asset_view_workflows";
            var resource = new Dictionary<string, object>();
            var permissionParam = new PermissionParam();
            if (command.WorkFlowType == WorkFlowType.Credential)
            {
                permissionParam = await _mediator.Send(new GetAssetGuidByCredentialGuidQuery(command.EntityId), cancellationToken);
                resource = new Dictionary<string, object>()
                {
                    { "entityId", permissionParam.EntityId ?? string.Empty },
                    { "workflowType", command.WorkFlowType.ToString() },
                    { "assetRegisterId", permissionParam.AssetRegisterId ?? string.Empty }
                };
            }
            else if (command.WorkFlowType == WorkFlowType.OwnerLeaseApproval)
            {
                var lease = await _mediator.Send(new GetAssetLeaseByIdQuery()
                {
                    AssetLeaseId = command.EntityId
                }, cancellationToken);

                permissionParam = new PermissionParam()
                {
                    EntityId = lease.AssetLeaseId,
                    AssetRegisterId = lease.AssetRegisterId
                };
                resource = new Dictionary<string, object>()
                {
                    { "entityId", permissionParam.EntityId ?? string.Empty },
                    { "workflowType", command.WorkFlowType.ToString() },
                    { "assetRegisterId", permissionParam.AssetRegisterId ?? string.Empty },
                    { "level", 1 }
                };
            }
            else if (command.WorkFlowType == WorkFlowType.Lease)
            {
                var lease = await _mediator.Send(new GetAssetLeaseByIdQuery()
                {
                    AssetLeaseId = command.EntityId
                }, cancellationToken);

                permissionParam = new PermissionParam()
                {
                    EntityId = lease.AssetLeaseId,
                    AssetRegisterId = lease.AssetRegisterId
                };
                resource = new Dictionary<string, object>()
                {
                    { "entityId", permissionParam.EntityId ?? string.Empty },
                    { "workflowType", command.WorkFlowType.ToString() },
                    { "assetRegisterId", permissionParam.AssetRegisterId ?? string.Empty },
                    { "level", 2 }
                };
            }
            else if (command.WorkFlowType == WorkFlowType.OwnerTransferApproval)
            {
                var transfer = await _mediator.Send(new GetAssetTransferByIdQuery()
                {
                    AssetTransferId = command.EntityId
                }, cancellationToken);

                permissionParam = new PermissionParam()
                {
                    EntityId = transfer.AssetTransferId,
                    AssetRegisterId = transfer.AssetRegisterId
                };
                resource = new Dictionary<string, object>()
                {
                    { "entityId", permissionParam.EntityId ?? string.Empty },
                    { "workflowType", command.WorkFlowType.ToString() },
                    { "assetRegisterId", permissionParam.AssetRegisterId ?? string.Empty },
                    { "level", 1 }
                };
            }
            else if (command.WorkFlowType == WorkFlowType.Transfer)
            {
                var transfer = await _mediator.Send(new GetAssetTransferByIdQuery()
                {
                    AssetTransferId = command.EntityId
                }, cancellationToken);

                permissionParam = new PermissionParam()
                {
                    EntityId = transfer.AssetTransferId,
                    AssetRegisterId = transfer.AssetRegisterId
                };
                resource = new Dictionary<string, object>()
                {
                    { "entityId", permissionParam.EntityId ?? string.Empty },
                    { "workflowType", command.WorkFlowType.ToString() },
                    { "assetRegisterId", permissionParam.AssetRegisterId ?? string.Empty },
                    { "level", 2 }
                };
            }
            else
            {
                resource = new Dictionary<string, object>()
                {
                    { "entityId", command.EntityId },
                    { "workflowType", command.WorkFlowType.ToString() }
                };
            }
            var isPermitted = await _abacPolicyEvaluatorService.EvaluatePolicyAsync(policyName, "submit", resource, cancellationToken);
            if (!isPermitted)
            {
                throw new ForbiddenAccessException();
            }
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<object, object>(result));
        }

    }
}
