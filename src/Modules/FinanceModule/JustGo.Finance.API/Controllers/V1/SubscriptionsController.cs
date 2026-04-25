using Asp.Versioning;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.DTOs.InstallmentDTOs;
using JustGo.Finance.Application.Features.Installments.Commands.CancelledInstallment;
using JustGo.Finance.Application.Features.Installments.Commands.CancelPlan;
using JustGo.Finance.Application.Features.Installments.Queries.ExportInstallments;
using JustGo.Finance.Application.Features.Subscriptions.Queries.GetSubscriptions;
using JustGo.Finance.Application.Features.Subscriptions.Queries.GetSubscriptionsPlan;
using JustGo.Finance.Application.Features.Subscriptions.Queries.GetSubscriptionsPlanBillingHistory;
using JustGo.Finance.Application.Features.Subscriptions.Queries.GetSubscriptionsPlansDetails;
using JustGo.Finance.Application.Features.Subscriptions.Queries.GetSubscriptionStatus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JustGo.Finance.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/subscriptions")]
    [ApiController]


    [Tags("Finance/Subscriptions")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;
        private readonly ILogger<SubscriptionsController> _logger;

        public SubscriptionsController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService, ILogger<SubscriptionsController> logger)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
            _logger = logger;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetSubscriptionStatus(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSubscriptionStatusQuery(), cancellationToken);
            if (result is null)
            {
                throw new NotFoundException();
            }
            return Ok(new ApiResponse<object, object>(result));
        }

        [HttpGet("plan-names/{merchantId}")]
        public async Task<IActionResult> GetSubscriptionPlan(Guid merchantId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSubscriptionsPlanQuery(merchantId), cancellationToken);
            if (result is null)
            {
                return new EmptyResult();
            }
            return Ok(new ApiResponse<object, object>(result));
        }
        [HttpPost("list")]
        public async Task<IActionResult> GetSubscriptions(GetSubscriptionsQuery request, CancellationToken cancellationToken)
        {

            var result = await _mediator.Send(request, cancellationToken);
            if (result is null)
            {
                throw new NotFoundException();
            }
            return Ok(new ApiResponse<object, object>(result));
        }
        [HttpPost("export")]
        public async Task<IActionResult> ExportInstallments([FromBody] GetInstallmentFilter filter, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new ExportInstallmentsQuery(filter, RecurringType.Subscription), cancellationToken);

            if (result is null)
            {
                throw new NotFoundException();
            }
            return Ok(new ApiResponse<object, object>(result));
        }

        [HttpGet("plans/{merchantId}/{id}/details")]
        public async Task<IActionResult> GetSubscriptionsPlansDetails(Guid merchantId, Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSubscriptionsPlansDetailsQuery(merchantId, id), cancellationToken);
            if (result is null || (result is IEnumerable<object> collection && !collection.Any()))
            {
                return new EmptyResult();
            }
            return Ok(new ApiResponse<object, object>(
                        result,
                        200,
                        "Plan details retrieved successfully."
                    ));
        }
        [HttpPost("plans/{merchantId}/{id}/billing-history")]
        public async Task<IActionResult> GetSubscriptionsPlanBillingHistory(Guid merchantId, Guid id, [FromBody] PlanHistoryRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSubscriptionsPlanBillingHistoryQuery(merchantId, id, request.SearchText,RecurringType.Subscription ,request.PageNo, request.PageSize, request.ColumnName, request.OrderBy), cancellationToken);
            if (result is null || (result is IEnumerable<object> collection && !collection.Any()))
            {
                return NotFound(new ApiResponse<object, object>(
                       new List<string> { "Plan not found for the given Merchant ID and Plan ID." },
                       404,
                       "Plan details not found."));
            }
            return Ok(new ApiResponse<object, object>(
                        result,
                        200,
                        "Plan details retrieved successfully."
                    ));
        }
        [HttpPatch("plans/{merchantId}/{id}/active-status")]
        public async Task<IActionResult> ChangePlanStatus(Guid merchantId, Guid id, [FromBody] ChangePlanStatusRequest request)
        {
            var command = new UpdatePlanStatusCommand(merchantId, id, request.IsActive, RecurringType.Subscription);
            var result = await _mediator.Send(command);
            if (!result)
            {
                return NotFound(new ApiResponse<object, object>(
                        new List<string> { "Plan not found or status unchanged." },
                        404,
                        "Failed to update plan status."));
            }

            return Ok(new ApiResponse<object, object>(
                       true,
                       200,
                       "Plan active status updated successfully."));
        }
        [HttpPatch("plans/{merchantId}/{id}/cancel")]
        public async Task<IActionResult> CancelPlan(Guid merchantId, Guid id, [FromBody] CancelPlanRequest request)
        {
            var command = new CancelPlanCommand(merchantId, id, RecurringType.Subscription, request.CancellationReason);
            var result = await _mediator.Send(command);
            if (!result)
            {
                return NotFound(new ApiResponse<object, object>(
                        new List<string> { "No matching plan was cancelled." },
                        404,
                        "Cancellation failed due to no matching plan."));
            }

            return Ok(new ApiResponse<object, object>(
                        true,
                        200,
                        "Plan cancelled successfully."));
        }

    }
}
