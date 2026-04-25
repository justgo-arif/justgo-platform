using JustGo.Finance.Application.DTOs.Balances;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Balances.Queries.GetAdyenBalances;

public class GetAdyenBalancesQuery : IRequest<MerchantBalanceDTO>
{
    public string BalanceAccountId { get; set; }

    public GetAdyenBalancesQuery(string id)
    {
        this.BalanceAccountId = id;
    }
}
