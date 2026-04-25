using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Installments.Queries.GetInstallmentPlan
{
    public class GetInstallmentPlanNameQuery : IRequest<List<LookupStringDto>>
    {
        public GetInstallmentPlanNameQuery(Guid merchantId)
        {
            MerchantId = merchantId;
        }

        public Guid MerchantId { get; set; }
    }
}
