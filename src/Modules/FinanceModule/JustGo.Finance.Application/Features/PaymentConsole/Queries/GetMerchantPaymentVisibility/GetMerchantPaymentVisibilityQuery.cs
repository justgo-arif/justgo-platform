using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs.PaymentConsoleDtos;

namespace JustGo.Finance.Application.Features.PaymentConsole.Queries.GetMerchantPaymentVisibility
{

    public class GetMerchantPaymentVisibilityQuery : IRequest<MerchantPaymentVisibilityDto>
    {
        public Guid MerchantId { get; set; }

        public GetMerchantPaymentVisibilityQuery(Guid merchantId)
        {
            MerchantId = merchantId;
        }
    }
}
