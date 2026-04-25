using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs.InstallmentDTOs;

namespace JustGo.Finance.Application.Features.Installments.Queries.GetInstallmentPlansDetails
{
    public class GetInstallmentPlansDetailsQuery : IRequest<InstallmentResponse>
    {
        public Guid MerchantId { get; set; }
        public Guid PlanId { get; set; }

        public GetInstallmentPlansDetailsQuery(Guid merchantId, Guid planId)
        {
            MerchantId = merchantId;
            PlanId = planId;
        }
    }
}
