using JustGo.Finance.Application.DTOs.PaymentAccount;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentAccount.Queries.GetAdyenPaymentAccountDetails;

public class GetAdyenPaymentAccountDetailsQuery : IRequest<AdyenPaymentProfileDetailsDTO?>
{
    public Guid MerchantId { get; set; }

    public GetAdyenPaymentAccountDetailsQuery(Guid merchantGuid)
    {
        this.MerchantId = merchantGuid;
    }
}
