using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs.PaymentAccount;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentAccount.Queries.GetSavedAdyenAccountDetails;

public class GetSavedAdyenAccountDetailsHandler : IRequestHandler<GetSavedAdyenAccountDetailsQuery, AdyenAccountDTO?>
{
    private readonly LazyService<IReadRepository<AdyenAccountDTO>> _readRepository;

    public GetSavedAdyenAccountDetailsHandler(LazyService<IReadRepository<AdyenAccountDTO>> readRepository)
    {
        _readRepository = readRepository;
    }
    public async Task<AdyenAccountDTO?> Handle(GetSavedAdyenAccountDetailsQuery request, CancellationToken cancellationToken)
    {
        var query = @"
                    SELECT TOP 1
                        aa.LegalEntityId,
	                    aa.BusinessLineId,
	                    aa.StoreId,
                        aba.PayoutSchedule,
                        aba.SweepId,
	                    aa.StatementDescriptor
                    FROM AdyenAccounts aa
                        INNER JOIN AdyenAccountHolders aah ON aah.AdyenAccountsId = aa.AdyenAccountsId
                        INNER JOIN AdyenBalanceAccounts aba ON aba.AdyenAccountHoldersId = aah.AdyenAccountHoldersId
                        INNER JOIN MerchantProfile_Default mpd ON aa.EntityId = mpd.DocId
                        INNER JOIN Document d ON mpd.DocId = d.DocId
                    WHERE d.SyncGuid = @MerchantId
                ";

        var queryParameters = new DynamicParameters();
        queryParameters.Add("MerchantId", request.MerchantId);

        var data = await _readRepository.Value.GetAsync(query, cancellationToken, queryParameters, null, "text");
        if (data is null)
        {
            return null;
        }
        return (AdyenAccountDTO)data;
    }
}
