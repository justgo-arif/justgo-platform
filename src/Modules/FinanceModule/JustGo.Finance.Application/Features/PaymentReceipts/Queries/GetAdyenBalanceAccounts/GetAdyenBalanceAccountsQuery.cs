using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetAdyenBalanceAccounts
{
    public class GetAdyenBalanceAccountsQuery : IRequest<List<AdyenBalanceAccountDTO>>
    {
        public Guid MerchantGuid { get; set; }

        public GetAdyenBalanceAccountsQuery(Guid merchantGuid)
        {
            this.MerchantGuid = merchantGuid;
        }
    }
}
