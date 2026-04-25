using JustGo.Finance.Application.DTOs.PaymentRefundDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentRefund.Commands.CreateRefundPayment
{
    public class CreateRefundPaymentCommand : RefundPaymentDto, IRequest<string>
    {
    }
}
