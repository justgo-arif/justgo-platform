using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Finance.Application.DTOs.PaymentConsoleDtos;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentConsole.Queries.GetMerchantPaymentEligibility;

public class GetMerchantPaymentEligibilityQuery : IRequest<MerchantPaymentEligibilityDto>
{
    public Guid MerchantId { get; set; }

    public GetMerchantPaymentEligibilityQuery(Guid merchantId)
    {
        MerchantId = merchantId;
    }
}
