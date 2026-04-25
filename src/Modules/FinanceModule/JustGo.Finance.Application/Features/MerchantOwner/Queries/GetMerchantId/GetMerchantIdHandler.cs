using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.Common.Constants;
using JustGo.Finance.Application.Common.Helpers;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.MerchantOwner.Queries.GetMerchantId
{
    public class GetMerchantIdHandler : IRequestHandler<GetMerchantIdQuery, int>
    {
        private readonly LazyService<IReadRepository<MerchantLookup>> _readRepository;

        public GetMerchantIdHandler(LazyService<IReadRepository<MerchantLookup>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<int> Handle(GetMerchantIdQuery request, CancellationToken cancellationToken)
        {
            var result = await _readRepository.Value.GetSingleAsync(
                    SqlQueries.GetMerchantIdSQL,
                    cancellationToken,
                    QueryHelpers.GetGuidParams(request.MerchantSyncGuid),
                    null,
                    "text"
            );
            if (result is null)
            {
                throw new InvalidOperationException("Merchant Id is not found");
            }
            return Convert.ToInt32(result);
        }
    }
}
