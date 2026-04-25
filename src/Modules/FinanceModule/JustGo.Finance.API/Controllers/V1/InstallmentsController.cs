using Asp.Versioning;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.DTOs.InstallmentDTOs;
using JustGo.Finance.Application.Features.Installments.Commands.CancelledInstallment;
using JustGo.Finance.Application.Features.Installments.Commands.CancelPlan;
using JustGo.Finance.Application.Features.Installments.Commands.UpdatePaymentSchedule;
using JustGo.Finance.Application.Features.Installments.Queries.ExportInstallments;
using JustGo.Finance.Application.Features.Installments.Queries.GetInstallment;
using JustGo.Finance.Application.Features.Installments.Queries.GetInstallmentPlan;
using JustGo.Finance.Application.Features.Installments.Queries.GetInstallmentPlanBillingHistory;
using JustGo.Finance.Application.Features.Installments.Queries.GetInstallmentPlansDetails;
using JustGo.Finance.Application.Features.Installments.Queries.GetInstallmentUpcomingSchedule;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace JustGo.Finance.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/installments")]
    [ApiController]


    [Tags("Finance/Installments")]
    public class InstallmentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;
        private readonly ILogger<InstallmentsController> _logger;

        public InstallmentsController(IMediator mediator, IAbacPolicyEvaluatorService abacPolicyEvaluatorService, ILogger<InstallmentsController> logger)
        {
            _mediator = mediator;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
            _logger = logger;
        }
        [HttpGet("plan-names/{merchantId}")]
        public async Task<IActionResult> GetInstallmentPlan(Guid merchantId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetInstallmentPlanNameQuery(merchantId), cancellationToken);
            if (result is null)
            {
                throw new NotFoundException();
            }
            return Ok(new ApiResponse<object, object>(result));
        }
        [HttpPost("list")]
        public async Task<IActionResult> GetInstallment(GetInstallmentQuery request, CancellationToken cancellationToken)
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
            var result = await _mediator.Send(new ExportInstallmentsQuery(filter, RecurringType.Installment), cancellationToken);

            if (result is null)
            {
                throw new NotFoundException();
            }
            return Ok(new ApiResponse<object, object>(result));
        }



        [HttpGet("plans/{merchantId}/{id}/details")]
        public async Task<IActionResult> GetInstallmentPlans(Guid merchantId, Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetInstallmentPlansDetailsQuery(merchantId, id), cancellationToken);
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
        [HttpPost("plans/{merchantId}/{id}/billing-history")]
        public async Task<IActionResult> GetInstallmentPlanbillinghistory(Guid merchantId, Guid id, [FromBody] PlanHistoryRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetInstallmentPlanBillingHistoryQuery(merchantId, id, request.SearchText, request.PageNo, request.PageSize, request.ColumnName, request.OrderBy), cancellationToken);
            if (result is null || (result is IEnumerable<object> collection && !collection.Any()))
            {
                return NotFound(new ApiResponse<object, object>(
                       new List<string> { $"Merchant Id  for Guid '{merchantId}' could not be found." },
                       404,
                       "Plan billing history not found."));
            }
            return Ok(new ApiResponse<object, object>(
                        result,
                        200,
                        "Plan billing history retrieved successfully."
                    ));
        }

        [HttpPost("plans/{merchantId}/{id}/upcoming")]
        public async Task<IActionResult> GetInstallmentUpcomingSchedule(Guid merchantId, Guid id, [FromBody] PlanHistoryRequest request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetInstallmentUpcomingScheduleQuery(merchantId, id, request.SearchText, request.PageNo, request.PageSize, request.ColumnName, request.OrderBy), cancellationToken);
            if (result is null || (result is IEnumerable<object> collection && !collection.Any()))
            {
                return NotFound(new ApiResponse<object, object>(
                    new List<string> { "No upcoming schedules found for the specified Merchant ID and Plan ID." },
                    404,
                    "No upcoming schedules found."));
            }

            return Ok(new ApiResponse<object, object>(
                result,
                200,
                "Upcoming schedules retrieved successfully."));

        }

        [HttpPatch("plans/{merchantId}/{id}/schedules")]
        public async Task<IActionResult> UpdatePaymentDateForPlan(Guid merchantId, int id, [FromBody] PaymentDateUpdateRequest request)
        {
            var command = new UpdatePaymentScheduleCommand(
                                merchantId,
                                id,
                               request
                            );
            var result = await _mediator.Send(command);
            if (!result)
            {
                return NotFound(new ApiResponse<object, object>(
                    new List<string> { "Payment schedule not found for this plan." },
                    404,
                    "Update failed."));
            }

            return Ok(new ApiResponse<object, object>(
                true,
                200,
                "Payment schedule updated successfully."));
        }

        [HttpPatch("plans/{merchantId}/{id}/active-status")]
        public async Task<IActionResult> ChangePlanStatus(Guid merchantId, Guid id, [FromBody] ChangePlanStatusRequest request)
        {
            var command = new UpdatePlanStatusCommand(merchantId, id, request.IsActive, RecurringType.Installment);
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
            var command = new CancelPlanCommand(merchantId, id, RecurringType.Installment, request.CancellationReason);
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
