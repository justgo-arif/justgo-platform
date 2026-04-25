using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.GetDocIdBySyncGuid
{
    public record GetDocIdBySyncGuidQuery(Guid SyncGuid) : IRequest<int>;
}
