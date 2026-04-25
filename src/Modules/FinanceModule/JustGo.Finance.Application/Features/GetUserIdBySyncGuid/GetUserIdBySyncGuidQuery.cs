using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.GetUserIdBySyncGuid
{
    public record GetUserIdBySyncGuidQuery(Guid SyncGuid) : IRequest<int>;
}
