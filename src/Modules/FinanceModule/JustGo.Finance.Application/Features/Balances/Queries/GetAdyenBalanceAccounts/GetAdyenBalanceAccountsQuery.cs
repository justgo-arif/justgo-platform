using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Balances.Queries.GetAdyenBalanceAccounts
{
    public class GetAdyenBalanceAccountsQuery : IRequest<List<AdyenBalanceAccountDTO>>
    {
        public Guid MerchantGuid { get; set; }

        public GetAdyenBalanceAccountsQuery(Guid merchantGuid)
        {
            MerchantGuid = merchantGuid;
        }
    }
}
