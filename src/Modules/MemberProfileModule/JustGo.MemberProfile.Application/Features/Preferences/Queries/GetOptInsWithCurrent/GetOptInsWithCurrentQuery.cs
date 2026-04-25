using JustGo.MemberProfile.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Preferences.GetOptInsWithCurrent
{
    public class GetOptInsWithCurrentQuery : IRequest<List<OptInMaster>>
    {
        public GetOptInsWithCurrentQuery(Guid syncGuid)
        {
            SyncGuid = syncGuid;
        }

        public Guid SyncGuid { get; set; }
    }
}


