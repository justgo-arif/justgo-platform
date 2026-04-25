using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Subscriptions.Queries.GetSubscriptionsPlan
{

    public class GetSubscriptionsPlanQuery : IRequest<List<LookupStringDto>?>
    {
        public GetSubscriptionsPlanQuery(Guid merchantId)
        {
            MerchantId = merchantId;
        }

        public Guid MerchantId { get; set; }
    }

}
