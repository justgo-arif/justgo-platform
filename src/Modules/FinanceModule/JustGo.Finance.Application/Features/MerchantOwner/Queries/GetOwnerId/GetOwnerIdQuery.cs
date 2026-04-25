using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.MerchantOwner.Queries.GetOwnerId
{
    public class GetOwnerIdQuery : IRequest<int>
    {
        public GetOwnerIdQuery(Guid ownerId)
        {
            OwnerId = ownerId;
        }
        public Guid OwnerId { get; set; }
    }
}
