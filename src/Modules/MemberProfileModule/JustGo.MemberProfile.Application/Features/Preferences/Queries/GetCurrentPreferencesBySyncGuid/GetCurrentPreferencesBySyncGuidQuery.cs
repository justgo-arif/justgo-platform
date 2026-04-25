using JustGo.MemberProfile.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Preferences.GetCurrentPreferencesBySyncGuid
{
    public class GetCurrentPreferencesBySyncGuidQuery : IRequest<CurrentPreference>
    {
        public GetCurrentPreferencesBySyncGuidQuery(Guid syncGuid)
        {
            SyncGuid = syncGuid;
        }

        public Guid SyncGuid { get; set; }
    }
}
