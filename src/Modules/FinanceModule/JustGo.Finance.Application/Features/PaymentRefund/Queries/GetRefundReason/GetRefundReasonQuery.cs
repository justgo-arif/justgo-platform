using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentRefund.Queries.GetRefundReason
{

    public class GetRefundReasonQuery : IRequest<List<LookupIntDto>>
    {
    }

}
