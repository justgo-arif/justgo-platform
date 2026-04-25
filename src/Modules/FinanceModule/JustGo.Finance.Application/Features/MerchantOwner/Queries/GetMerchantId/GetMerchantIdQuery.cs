using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.MerchantOwner.Queries.GetMerchantId
{
    public class GetMerchantIdQuery : IRequest<int>
    {
        public GetMerchantIdQuery(Guid MerchantId)
        {
            MerchantSyncGuid = MerchantId;
        }

        public Guid MerchantSyncGuid { get; set; }
    }
}
