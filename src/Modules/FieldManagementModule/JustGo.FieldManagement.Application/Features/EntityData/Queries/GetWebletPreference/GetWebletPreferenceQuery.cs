using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.FieldManagement.Domain.Entities;

namespace JustGo.FieldManagement.Application.Features.EntityData.Queries.GetWebletPreference;

public class GetWebletPreferenceQuery : IRequest<WebletPreference>
{
    public Guid UserSyncId { get; set; }
    public string PreferenceType { get; set; }

    public GetWebletPreferenceQuery(Guid userSyncId, string preferenceType)
    {
        UserSyncId = userSyncId;
        PreferenceType = preferenceType;
    }
}
