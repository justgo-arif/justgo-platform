using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.DTOs;

namespace JustGo.Finance.Application.Features.MerchantOwner.Queries.GetMerchantEntityIdentifiers
{
    public class GetMerchantEntityIdentifiersQuery : IRequest<MerchantEntityIdentifiersDto>
    {
        public GetMerchantEntityIdentifiersQuery(Guid merchantSyncGuid)
        {
            MerchantSyncGuid = merchantSyncGuid;
        }

        public Guid MerchantSyncGuid { get; set; }
    }
}
