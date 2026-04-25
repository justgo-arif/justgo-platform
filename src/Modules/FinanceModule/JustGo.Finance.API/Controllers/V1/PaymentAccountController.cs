using Asp.Versioning;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs.PaymentAccount;
using JustGo.Finance.Application.Features.PaymentAccount.Commands.UpdateStatementDescriptor;
using JustGo.Finance.Application.Features.PaymentAccount.Commands.UpdateSweep;
using JustGo.Finance.Application.Features.PaymentAccount.Queries.GetAdyenPaymentAccountDetails;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JustGo.Finance.API.Controllers.V1;

[ApiVersion("1.0")]
[Tags("Finance/PaymentAccount")]
[Route("api/v{version:apiVersion}/payment-accounts")]
[ApiController]
public class PaymentAccountController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentAccountController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("merchant/{merchantId}")]
    [ProducesResponseType(typeof(ApiResponse<AdyenPaymentProfileDetailsDTO, object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PaymentProfileDetails(Guid merchantId, CancellationToken cancellationToken)
    {
        var accounts = await _mediator.Send(new GetAdyenPaymentAccountDetailsQuery(merchantId), cancellationToken);
        if (accounts is null) return new EmptyResult();

        return Ok(new ApiResponse<AdyenPaymentProfileDetailsDTO, object>(accounts));
    }

    [HttpPut("sweep")]
    public async Task<IActionResult> UpdateSweep([FromBody] UpdateSweepCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (!result) return new EmptyResult();
        return Ok(new ApiResponse<bool, object>(result));
    }

    [HttpPatch("merchant/{merchantId}/statement-descriptor")]
    public async Task<IActionResult> UpdateStatementDescriptor(Guid merchantId, [FromBody] UpdateStatementDescriptorRequest request, CancellationToken cancellationToken)
    {
        var command = request.Adapt<UpdateStatementDescriptorCommand>();
        command.MerchantId = merchantId;
        var result = await _mediator.Send(command, cancellationToken);
        return result ? Ok(new ApiResponse<bool, object>(true)) : new EmptyResult();
    }  

}
