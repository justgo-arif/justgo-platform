using JustGo.Finance.Application.DTOs.InstallmentDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Subscriptions.Queries.GetSubscriptionsPlansDetails
{
    public class GetSubscriptionsPlansDetailsQuery : IRequest<InstallmentResponse?>
    {
        public Guid MerchantId { get; set; }
        public Guid PlanId { get; set; }

        public GetSubscriptionsPlansDetailsQuery(Guid merchantId, Guid planId)
        {
            MerchantId = merchantId;
            PlanId = planId;
        }
    }
}
