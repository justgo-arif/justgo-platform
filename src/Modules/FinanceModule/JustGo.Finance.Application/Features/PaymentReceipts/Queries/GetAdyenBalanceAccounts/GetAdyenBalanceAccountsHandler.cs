using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.DTOs.Balances;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetAdyenBalanceAccounts
{
    public class GetAdyenBalanceAccountsHandler : IRequestHandler<GetAdyenBalanceAccountsQuery, List<AdyenBalanceAccountDTO>>
    {
        private readonly LazyService<IReadRepository<AdyenBalanceAccountDTO>> _readRepository;

        public GetAdyenBalanceAccountsHandler(LazyService<IReadRepository<AdyenBalanceAccountDTO>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<AdyenBalanceAccountDTO>> Handle(GetAdyenBalanceAccountsQuery request, CancellationToken cancellationToken)
        {
            var queryParameters = new DynamicParameters();
            queryParameters.Add("MerchantId", request.MerchantGuid);

            var query = @"
                SELECT 
                    aba.BalanceAccountId,
	                aba.Currency,
	                aba.[Status],
	                aba.SweepId,
	                aba.PayoutSchedule 
                FROM AdyenAccounts aa
                    INNER JOIN AdyenAccountHolders aah ON aa.AdyenAccountsId = aah.AdyenAccountsId
                    INNER JOIN AdyenBalanceAccounts aba ON aah.AdyenAccountHoldersId = aba.AdyenAccountHoldersId
                    INNER JOIN MerchantProfile_Default mpd ON aa.EntityId = mpd.DocId
                    INNER JOIN Document d ON mpd.DocId = d.DocId
                WHERE d.SyncGuid = @MerchantId
            ";

            var data = await _readRepository.Value
                .GetListAsync(query, cancellationToken, queryParameters, null, "text");

            return data.ToList();

        }
    }
}
