using Adyen.Service.BalancePlatform;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs.Balances;
using JustGo.Finance.Application.Interfaces;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.Balances.Queries.GetAdyenBalances;

public class GetAdyenBalancesHandler : IRequestHandler<GetAdyenBalancesQuery, MerchantBalanceDTO?>
{
    private readonly LazyService<IReadRepository<MerchantBalanceDTO>> _readRepository;
    private readonly IAdyenClientFactory _adyenClientFactory;


    public GetAdyenBalancesHandler(
        LazyService<IReadRepository<MerchantBalanceDTO>> readRepository, 
        IAdyenClientFactory adyenClientFactory
        )
    {
        _readRepository = readRepository;
        _adyenClientFactory = adyenClientFactory;
    }

    public async Task<MerchantBalanceDTO?> Handle(GetAdyenBalancesQuery request, CancellationToken cancellationToken)
    {
        var balances = await GetBalancesFromAdyen(request.BalanceAccountId, cancellationToken);
        return balances;
    }

    private async Task<MerchantBalanceDTO?> GetBalancesFromAdyen(string balanceAccountId, CancellationToken cancellationToken)
    {
        try
        {
            var client = await _adyenClientFactory.CreateClientAsync(DTOs.Enums.AdyenKeyType.BalancePlatform, cancellationToken);

            if (client is null) return null;

            var service = new BalanceAccountsService(client);
            var response = await service.GetBalanceAccountAsync(balanceAccountId);

            if(response is null) return null;

            var accountBalance = new MerchantBalanceDTO
            {
                Status = response.Status,
                AccountHolderId = response.AccountHolderId,
                DefaultCurrencyCode = response.DefaultCurrencyCode,
                Description = response.Description,
                Id = response.Id,
                MigratedAccountCode = response.MigratedAccountCode,
                Reference = response.Reference,
                TimeZone = response.TimeZone,
                Balances = response.Balances?.Select(b => new BalanceDto
                {
                    Available = (decimal)(b.Available ?? 0) / 100,
                    _Balance = ((decimal)(b.Available ?? 0) / 100) + ((decimal)(b.Pending ?? 0) / 100),
                    Currency = b.Currency,
                    Pending = (decimal)(b.Pending ?? 0) / 100,
                    Reserved = (decimal)(b.Reserved ?? 0) / 100
                }).ToList()
            };

            return accountBalance;
        }
        catch (Adyen.HttpClient.HttpClientException)
        {
            return null;
        }
    }
}
