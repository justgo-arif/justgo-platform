using Asp.Versioning;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.DTOs.PaymentConsoleDtos;
using JustGo.Finance.Application.Features.PaymentConsole.Commands.CreatePaymentConsole;
using JustGo.Finance.Application.Features.PaymentConsole.Queries.GetMerchantPaymentEligibility;
using JustGo.Finance.Application.Features.PaymentConsole.Queries.GetMerchantPaymentVisibility;
using JustGo.Finance.Application.Features.PaymentConsole.Queries.GetPaymentMethods;
using JustGo.Finance.Application.Features.PaymentConsole.Queries.GetPaymentTypes;
using JustGo.Finance.Application.Features.PaymentConsole.Queries.GetProductCategory;
using JustGo.Finance.Application.Features.PaymentConsole.Queries.GetUserPaymentInfo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.Finance.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/paymentconsole")]
    [ApiController]


    public class PaymentConsoleController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICustomError _error;

        public PaymentConsoleController(IMediator mediator, ICustomError error)
        {
            _mediator = mediator;
            _error = error;
        }

        [Tags("Finance/PaymentConsole")]
        [HttpPost("payment-user-info")]
        public async Task<IActionResult> GetUserPaymentInfo(GetUserPaymentInfoQuery request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            if (result is null)
            {
                _error.NotFound<object>($"Payment user info not found for the given userid.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<object, object>(result));
        }

        [Tags("Finance/PaymentConsole")]
        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePaymentConsole(CreatePaymentConsoleCommand request, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            if (result is not "Success")
            {
                return new EmptyResult();
            }
            return Ok(new ApiResponse<object, object>(result));
        }

        [Tags("Finance/PaymentConsole")]
        [HttpGet("product-category")]
        public async Task<IActionResult> GetProductCategory(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetProductCategoryQuery(), cancellationToken);
            if (result is null)
            {
                _error.NotFound<object>($"Product Category not found.");
                return new EmptyResult();
            }
            return Ok(new ApiResponse<object, object>(result));
        }

        [Tags("Finance/PaymentConsole")]
        [HttpGet("payment-methods")]
        public async Task<IActionResult> GetPaymentMethods(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetPaymentMethodsQuery(), cancellationToken);
            return Ok(new ApiResponse<List<LookupIntDto>, object>(result));
        }

        [Tags("Finance/PaymentConsole")]
        [HttpGet("payment-types")]
        public async Task<IActionResult> GetPaymentTypes(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetConsolePaymentTypesQuery(), cancellationToken);
            return Ok(new ApiResponse<List<ConsolePaymentTypesDto>, object>(result));
        }

        [Tags("Finance/PaymentConsole")]
        [HttpGet("eligibility/{merchantId}")]
        public async Task<IActionResult> GetMerchantPaymentEligibility(Guid merchantId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMerchantPaymentEligibilityQuery(merchantId), cancellationToken);
            return Ok(new ApiResponse<MerchantPaymentEligibilityDto, object>(result));
        }
        [Tags("Finance/PaymentConsole")]
        [HttpGet("visibility/{merchantId}")]
        public async Task<IActionResult> GetMerchantPaymentVisibility(Guid merchantId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMerchantPaymentVisibilityQuery(merchantId), cancellationToken);
            return Ok(new ApiResponse<MerchantPaymentVisibilityDto, object>(result));
        }

    }
}
