using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Finance.Application.DTOs.PaymentAccount;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentAccount.Queries.GetSavedAdyenAccountDetails;

public class GetSavedAdyenAccountDetailsQuery : IRequest<AdyenAccountDTO?>
{
    public Guid MerchantId { get; set; }

    public GetSavedAdyenAccountDetailsQuery(Guid merchantId)
    {
        this.MerchantId = merchantId;
    }
}
