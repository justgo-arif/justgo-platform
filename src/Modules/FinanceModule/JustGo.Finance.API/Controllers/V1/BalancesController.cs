using Asp.Versioning;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs.Balances;
using JustGo.Finance.Application.Features.Balances.Queries.GetAdyenBalances;
using JustGo.Finance.Application.Features.Balances.Queries.GetAdyenPayouts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.Finance.API.Controllers.V1
{
    [ApiVersion("1.0")]
    [Tags("Finance/Balances")]
    [Route("api/v{version:apiVersion}/balances")]
    [ApiController]
    public class BalancesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BalancesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("account/{id}")]
        [ProducesResponseType(typeof(ApiResponse<MerchantBalanceDTO, object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMerchantBalances(string id, CancellationToken cancellationToken)
        {
            var accounts = await _mediator.Send(new GetAdyenBalancesQuery(id), cancellationToken);
            return Ok(new ApiResponse<MerchantBalanceDTO, object>(accounts));
        }

        [HttpPost("adyen-payouts")]
        [ProducesResponseType(typeof(ApiResponse<AdyenPayoutsDTO, object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAdyenPayouts(GetAdyenPayoutsQuery query, CancellationToken cancellationToken)
        {
            var accounts = await _mediator.Send(query, cancellationToken);
            return Ok(new ApiResponse<AdyenPayoutsDTO, object>(accounts));
        }

    }
}
