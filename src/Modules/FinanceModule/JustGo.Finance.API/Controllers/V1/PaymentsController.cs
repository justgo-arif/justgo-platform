using System.Threading;
using Adyen.Model.Management;
using Asp.Versioning;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.DTOs.InstallmentDTOs;
using JustGo.Finance.Application.DTOs.MemberPaymentDTOs;
using JustGo.Finance.Application.DTOs.PaymentRefundDTOs;
using JustGo.Finance.Application.DTOs.ProductDTOs;
using JustGo.Finance.Application.DTOs.RefundPaymentDTOs;
using JustGo.Finance.Application.Features.Installments.Commands.CancelledInstallment;
using JustGo.Finance.Application.Features.Installments.Commands.CancelPlan;
using JustGo.Finance.Application.Features.MemberPayments.GetMemberPaymentDetails;
using JustGo.Finance.Application.Features.MemberPayments.GetMemberPaymentOverview;
using JustGo.Finance.Application.Features.MemberPayments.GetMemberPayments;
using JustGo.Finance.Application.Features.MemberPayments.GetMemberPaymentSummary;
using JustGo.Finance.Application.Features.MemberPayments.GetMemberPlanDetails;
using JustGo.Finance.Application.Features.MemberPayments.GetMemberPlans;
using JustGo.Finance.Application.Features.Members.Queries.GetMemberList;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.ExportReceipts;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetAllPaymentMethods;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentDetails;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentLog;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentMethod;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentOverview;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentProduct;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentReceipts;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentStatus;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentSummary;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentTerminalDetails;
using JustGo.Finance.Application.Features.PaymentRefund.Commands.CreateRefundPayment;
using JustGo.Finance.Application.Features.PaymentRefund.Queries.GetRefundHistory;
using JustGo.Finance.Application.Features.PaymentRefund.Queries.GetRefundProduct;
using JustGo.Finance.Application.Features.PaymentRefund.Queries.GetRefundReason;
using JustGo.Finance.Application.Features.Plans.Queries.GetPlanOwner;
using JustGo.Finance.Application.Features.Subscriptions.Queries.GetSubscriptionsPlanBillingHistory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
namespace JustGo.Finance.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/payments")]
    [ApiController]


    public class PaymentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICustomError _error;
        private readonly IAbacPolicyEvaluatorService _abacPolicyEvaluatorService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(IMediator mediator, ICustomError error, IAbacPolicyEvaluatorService abacPolicyEvaluatorService, ILogger<PaymentsController> logger)
        {
            _mediator = mediator;
            _error = error;
            _abacPolicyEvaluatorService = abacPolicyEvaluatorService;
            _logger = logger;
        }

        [Tags("Finance/Payments")]
        //[CustomAuthorize("allowMemberProfileView", "view", "guid")]
        [HttpPost("receipt-list")]
        public async Task<IActionResult> GetPaymentReceipts(GetPaymentReceiptsQuery request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            if (result is null)
            {
                _error.NotFound<object>("No data found.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<object, object>(result));
        }

        [Tags("Finance/Payments")]
        //[CustomAuthorize("allowMemberProfileView", "view", "guid")]
        [HttpPost("members")]
        public async Task<IActionResult> GetMembers(GetMemberListQuery request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            if (result is null)
            {
                _error.NotFound<object>("No data found.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<object, object>(result));
        }
        [Tags("Finance/Payments")]
        [HttpPost("export-receipts")]
        public async Task<IActionResult> ExportReceiptsAsCsv([FromBody] ExportReceiptsQuery request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            if (result is null)
            {
                _error.NotFound<object>("No data found.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<object, object>(result));
        }



        [Tags("Finance/Payments")]
        [HttpPost("products")]
        public async Task<IActionResult> GetPaymentProduct(GetPaymentProductRequestQuery request, CancellationToken cancellationToken)
        {
            var query = new GetPaymentProductQuery(
                merchantId: request.MerchantId,
                memberId: null,
                paymentId: request.PaymentId,
                searchText: request.SearchText,
                pageNo: request.PageNo,
                pageSize: request.PageSize,
                source: ProductRequestSource.Merchant
            );
            var result = await _mediator.Send(query, cancellationToken);
            if (result is null)
            {
                _error.NotFound<object>("No data found.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<object, object>(result));
        }
        [Tags("Finance/Payments")]
        //[CustomAuthorize("allowMemberProfileView", "view", "guid")]
        [HttpGet("status")]
        public async Task<IActionResult> GetPaymentStatus(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPaymentStatusQuery(), cancellationToken);
            if (result is null)
            {
                return NotFound(new ApiResponse<object, object>(
                   new List<string> { "Payment status not found." },
                   404,
                   "Payment status not found."));
            }
            return Ok(new ApiResponse<object, object>(result));
        }

        [Tags("Finance/Payments")]
        [HttpGet("paymentmethods")]
        public async Task<IActionResult> GetAllPaymentMethods(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllPaymentMethodsQuery(), cancellationToken);
            if (result is null)
            {
                _error.NotFound<object>("Payment methods not found.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<object, object>(result));
        }



        [Tags("Finance/Payments")]
        [HttpGet("overview/{merchantId}/{paymentId}")]
        public async Task<IActionResult> GetPaymentOverview(Guid merchantId, Guid paymentId)
        {
            var result = await _mediator.Send(new GetPaymentOverviewQuery(merchantId, paymentId));

            if (result == null)
                return NotFound();
            return Ok(new ApiResponse<PaymentOverviewDto, object>(result));
        }
        [Tags("Finance/Payments")]
        [HttpGet("receipt/{merchantId}/{paymentId}")]
        public async Task<IActionResult> GetPaymentSummary(Guid merchantId, Guid paymentId, CancellationToken cancellationToken)
        {
            var query = new GetPaymentSummaryQuery(paymentId)
            {
                MerchantId = merchantId,
                Source = RequestSource.Merchant
            };
            var result = await _mediator.Send(query, cancellationToken);

            if (result is null)
            {
                _error.NotFound<object>("No data found.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<PaymentSummaryVM, object>(result));
        }

        [Tags("Finance/Payments")]
        [HttpGet("details/{merchantId}/{paymentId}")]
        public async Task<IActionResult> GetPaymentdetails(Guid merchantId, Guid paymentId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPaymentDetailsQuery(merchantId, paymentId), cancellationToken);

            if (result is null)
            {
                _error.NotFound<object>("No data found.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<PaymentDetailsVM, object>(result));
        }

        [Tags("Finance/Payments")]
        [HttpGet("{paymentId}/method")]
        public async Task<IActionResult> GetPaymentMethod(Guid paymentId)
        {
            var result = await _mediator.Send(new GetPaymentMethodQuery(paymentId));

            if (result == null)
            {
                return NotFound(new ApiResponse<object, object>(
                    new List<string> { "Payment method not found for the given Payment ID." },
                    404,
                    "Payment method not found."));
            }

            return Ok(new ApiResponse<object, object>(
                result,
                200,
                "Payment method retrieved successfully."));
        }
        [Tags("Finance/Payments")]
        [HttpGet("{paymentId}/terminal")]
        public async Task<IActionResult> GetPaymentTerminal(Guid paymentId)
        {
            var result = await _mediator.Send(new GetPaymentTerminalDetailsQuery(paymentId));

            if (result == null)
            {
                return NotFound(new ApiResponse<object, object>(
                    new List<string> { "Terminal information not found for this payment." },
                    404,
                    "Payment terminal not found."));
            }

            return Ok(new ApiResponse<object, object>(
                result,
                200,
                "Payment terminal retrieved successfully."));
        }
        [Tags("Finance/Payments")]
        [HttpGet("payment-log/{paymentId}")]
        public async Task<IActionResult> GetPaymentlogs(Guid paymentId, CancellationToken cancellationToken)
        {

            var result = await _mediator.Send(new GetPaymentLogQuery(paymentId), cancellationToken);

            if (result is null)
            {
                _error.NotFound<object>("No data found.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<List<PaymentLog>, object>(result));
        }
        [Tags("Finance/Refund")]
        [HttpGet("refund-reason")]
        public async Task<IActionResult> GetRefundReason(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetRefundReasonQuery(), cancellationToken);
            if (result is null)
            {
                _error.NotFound<object>("No data found.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<object, object>(result));
        }
        [Tags("Finance/Refund")]
        [HttpGet("refund-items/{merchantId}/{paymentId}")]
        public async Task<IActionResult> GetRefundableItems(Guid merchantId, Guid paymentId, CancellationToken cancellationToken)
        {
            var query = new GetRefundableItemsQuery(paymentId)
            {
                MerchantId = merchantId,
                Source = RequestSource.Merchant
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (result == null || !result.Any())
            {
                return NotFound(new ApiResponse<string, object>("No refundable items found."));
            }

            return Ok(new ApiResponse<List<RefundableItemDto>, object>(result));
        }


        [Tags("Finance/Refund")]
        [HttpPost("refunds")]
        public async Task<IActionResult> CreateRefund([FromBody] MerchantRefundPaymentDto dto, CancellationToken cancellationToken)
        {
            var refundType = dto.RequestRefundType == RefundType.Percentage
                ? RefundType.Partial
                : dto.RequestRefundType;
            var command = new CreateRefundPaymentCommand
            {
                Source = RequestSource.Merchant,
                MerchantId = dto.MerchantId,
                PaymentId = dto.PaymentId,
                RefundReasonId = dto.RefundReasonId,
                RefundNote = dto.RefundNote,
                IsSendNotification = dto.IsSendNotification,
                RefundItems = dto.RefundItems,
                RequestRefundType = refundType
            };
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new ApiResponse<string, object>($"Refund successfully created. Payment ID: {result}"));
        }

        [Tags("Finance/Refund")]
        [HttpPost("refund-history")]
        public async Task<IActionResult> GetRefundHistory(GetMerchantRefundHistoryQuery request, CancellationToken cancellationToken)
        {
            var query = new GetRefundHistoryQuery(
                RequestSource.Merchant,
                request.MerchantId,
                null,
                request.PaymentId,
                request.SearchText,
                request.ColumnName,
                request.OrderBy,
                request.PageNo,
                request.PageSize
            );
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                return NotFound(new ApiResponse<string, object>("No refund history found."));
            }

            return Ok(new ApiResponse<RefundInfoVM, object>(result));
        }

        [Tags("Finance/Payments")]
        [CustomAuthorize("member_view_payments", "view", "memberId")]
        [HttpPost("{memberId}/orders")]
        public async Task<IActionResult> GetMemberPayments(Guid memberId, GetMemberPaymentsRequest request, CancellationToken cancellationToken)
        {
            var query = new GetMemberPaymentsQuery(
                        userId: memberId,
                        fromDate: request.FromDate,
                        toDate: request.ToDate,
                        paymentMethods: request.PaymentMethods,
                        statusIds: request.StatusIds,
                        scopeKey: request.ScopeKey,
                        searchText: request.SearchText,
                        pageSize: request.PageSize,
                        lastPaymentId: request.LastPaymentId
                    );
            var result = await _mediator.Send(query, cancellationToken);
            if (result is null)
            {
                _error.NotFound<object>("No data found.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<object, object>(result));
        }

        [Tags("Finance/Payments")]
        [HttpGet("{memberId}/orders-receipt/{orderId}")]
        public async Task<IActionResult> GetOrdersSummary(Guid memberId, Guid orderId, CancellationToken cancellationToken)
        {
            var query = new GetPaymentSummaryQuery(orderId)
            {
                MemberId = memberId,
                Source = RequestSource.Member
            };
            var result = await _mediator.Send(query, cancellationToken);

            if (result is null)
            {
                _error.NotFound<object>("No data found.");
                return new EmptyResult();
            }

            var resource = new Dictionary<string, object>()
            {
                { "id", memberId }
            };
            var permissions = await _abacPolicyEvaluatorService.EvaluatePolicyAsync("member_profile_create_refund_ui", cancellationToken, null, resource);

            return Ok(new ApiResponse<PaymentSummaryVM, object>(result, permissions));
        }

        [Tags("Finance/Payments")]
        [HttpGet("{memberId}/orders-details/{orderId}")]
        public async Task<IActionResult> GetOrdersdetails(Guid memberId, Guid orderId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMemberPaymentDetailsQuery(memberId, orderId), cancellationToken);

            if (result is null)
            {
                _error.NotFound<object>("No data found.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<PaymentDetailsVM, object>(result));
        }
        [Tags("Finance/Payments")]
        [HttpGet("{memberId}/overview/{paymentId}")]
        public async Task<IActionResult> GetMemberPaymentOverview(Guid memberId, Guid paymentId)
        {
            var result = await _mediator.Send(new GetMemberPaymentOverviewQuery(memberId, paymentId));

            if (result == null)
                return NotFound();
            return Ok(new ApiResponse<PaymentOverviewDto, object>(result));
        }
        [Tags("Finance/Payments")]
        [HttpPost("{memberId}/products")]
        public async Task<IActionResult> GetMemberPaymentProduct(Guid memberId, GetMemberProductsRequestQuery request, CancellationToken cancellationToken)
        {
            var query = new GetPaymentProductQuery(
                merchantId: null,
                memberId: memberId,
                paymentId: request.PaymentId,
                searchText: request.SearchText,
                pageNo: request.PageNo,
                pageSize: request.PageSize,
                source: ProductRequestSource.Member
            );

            var result = await _mediator.Send(query, cancellationToken);
            if (result is null)
            {
                _error.NotFound<object>("No data found.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<object, object>(result));
        }
        [Tags("Finance/Payments")]
        [CustomAuthorize("member_view_subscriptions", "view", "memberId")]
        [HttpGet("{memberId}/plans")]
        public async Task<IActionResult> GetMemberPlans(Guid memberId, [FromQuery] string? status, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMemberPlansQuery(memberId, status), cancellationToken);

            if (result?.Categories == null || !result.Categories.Any())
            {
                return Ok(new ApiResponse<object, object>(result, "No plans found for this member."));
            }

            return Ok(new ApiResponse<object, object>(result, "Plans retrieved successfully."));
        }

        [Tags("Finance/Payments")]
        [HttpGet("{memberId}/plans/{planId}")]
        public async Task<IActionResult> GetMemberPlanDetails(Guid memberId, Guid planId, CancellationToken cancellationToken)
        {
            var query = new GetMemberPlanDetailsQuery(memberId, planId);
            var result = await _mediator.Send(query, cancellationToken);
            if (result is null || (result is IEnumerable<object> collection && !collection.Any()))
            {
                return NotFound(new ApiResponse<object, object>(
                       new List<string> { "Plan not found for the given Member ID and Plan ID." },
                       404,
                       "Plan details not found."));
            }
            var resource = new Dictionary<string, object>()
            {
                { "id", memberId }
            };
            var permissions = await _abacPolicyEvaluatorService.EvaluatePolicyAsync("member_profile_plan_actions_ui", cancellationToken, null, resource);

            return Ok(new ApiResponse<object, object>(
                        result, permissions,
                        200,
                        "Plan details retrieved successfully."
                    ));
        }

        [Tags("Finance/Refund")]
        [HttpGet("{memberId}/refund-items/{paymentId}")]
        public async Task<IActionResult> GetMemberRefundableItems(Guid memberId, Guid paymentId, CancellationToken cancellationToken)
        {
            var query = new GetRefundableItemsQuery(paymentId)
            {
                MemberId = memberId,
                Source = RequestSource.Member
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (result == null || !result.Any())
            {
                return NotFound(new ApiResponse<string, object>("No refundable items found."));
            }

            return Ok(new ApiResponse<List<RefundableItemDto>, object>(result));
        }
        [Tags("Finance/Refund")]
        [HttpPost("{memberId}/refunds")]
        public async Task<IActionResult> CreateMemberRefund(Guid memberId,[FromBody] MemberRefundPaymentDto dto,CancellationToken cancellationToken)
        {
            var refundType = dto.RequestRefundType == RefundType.Percentage
                ? RefundType.Partial
                : dto.RequestRefundType;

            var command = new CreateRefundPaymentCommand
            {
                Source = RequestSource.Member,
                MemberId = memberId,
                PaymentId = dto.PaymentId,
                RefundReasonId = dto.RefundReasonId,
                RefundNote = dto.RefundNote,
                IsSendNotification = dto.IsSendNotification,
                RefundItems = dto.RefundItems,
                RequestRefundType = refundType
            };

            var result = await _mediator.Send(command, cancellationToken);

            return Ok(new ApiResponse<string, object>(
                result,
                $"Refund successfully created. Payment ID: {result}"
            ));
        }

        [Tags("Finance/Refund")]
        [HttpPost("{memberId}/refund-history")]
        public async Task<IActionResult> GetMemberRefundHistory(Guid memberId, GetMemberRefundHistoryQuery request, CancellationToken cancellationToken)
        {
            var query = new GetRefundHistoryQuery(
                RequestSource.Member,
                null,
                memberId,
                request.PaymentId,
                request.SearchText,
                request.ColumnName,
                request.OrderBy,
                request.PageNo,
                request.PageSize
            );
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                return NotFound(new ApiResponse<string, object>("No refund history found."));
            }

            return Ok(new ApiResponse<RefundInfoVM, object>(result));
        }
        [Tags("Finance/Payments")]
        [HttpPost("{memberId}/plans/{planId}/billing-history")]
        public async Task<IActionResult> GetMemberPlanBillingHistory(Guid memberId, Guid planId, [FromBody] PlanHistoryRequest request, CancellationToken cancellationToken)
        {
            var planmerchant = await _mediator.Send(new GetPlanOwnerQuery(planId, memberId), cancellationToken);
            if (planmerchant == null)
            {
                return NotFound(new ApiResponse<object, object>(
                    new List<string> { "Plan not found for the given Member ID and Plan ID." },
                    404,
                    "Plan not found."
                ));
            }
            var result = await _mediator.Send(new GetSubscriptionsPlanBillingHistoryQuery(planmerchant.MerchantId, planId, request.SearchText, (RecurringType)planmerchant.RecurringType, request.PageNo, request.PageSize, request.ColumnName, request.OrderBy), cancellationToken);
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
        [Tags("Finance/Payments")]
        [HttpPatch("{memberId}/plans/{planId}/active-status")]
        public async Task<IActionResult> ChangePlanStatus(Guid memberId, Guid planId, [FromBody] ChangePlanStatusRequest request, CancellationToken cancellationToken)
        {
            var planmerchant = await _mediator.Send(new GetPlanOwnerQuery(planId, memberId), cancellationToken);
            if (planmerchant == null)
            {
                return NotFound(new ApiResponse<object, object>(
                    new List<string> { "Plan not found for the given Member ID and Plan ID." },
                    404,
                    "Plan not found."
                ));
            }
            var command = new UpdatePlanStatusCommand(planmerchant.MerchantId, planId, request.IsActive, (RecurringType)planmerchant.RecurringType);
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
        [Tags("Finance/Payments")]
        [HttpPatch("{memberId}/plans/{planId}/cancel")]
        public async Task<IActionResult> CancelPlan(Guid memberId, Guid planId, [FromBody] CancelPlanRequest request)
        {
            var planmerchant = await _mediator.Send(new GetPlanOwnerQuery(planId, memberId), CancellationToken.None);
            if (planmerchant == null)
            {
                return NotFound(new ApiResponse<object, object>(
                    new List<string> { "Plan not found for the given Member ID and Plan ID." },
                    404,
                    "Plan not found."
                ));
            }
            var resource = new Dictionary<string, object>()
            {
                { "id", memberId }
            };
            var permissions = await _abacPolicyEvaluatorService.EvaluatePolicyAsync("member_profile_plan_actions_ui", CancellationToken.None, null, resource);
            if (!permissions.TryGetValue("cancelled_plan", out var cancelledPlanPerm) || !cancelledPlanPerm.View)
            {
                return Forbid("You do not have permission to cancel this plan.");
            }
            var command = new CancelPlanCommand(planmerchant.MerchantId, planId, (RecurringType)planmerchant.RecurringType, request.CancellationReason);
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
