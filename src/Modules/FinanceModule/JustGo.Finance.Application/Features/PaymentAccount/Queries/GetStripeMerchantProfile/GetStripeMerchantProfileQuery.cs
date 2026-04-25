using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Finance.Application.DTOs.PaymentAccount;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentAccount.Queries.GetStripeMerchantProfile;

public class GetStripeMerchantProfileQuery : IRequest<StripeMerchantProfileDTO>
{
    public int MerchantId { get; set; }

    public GetStripeMerchantProfileQuery(int merchantGuid)
    {
        this.MerchantId = merchantGuid;
    }
}
