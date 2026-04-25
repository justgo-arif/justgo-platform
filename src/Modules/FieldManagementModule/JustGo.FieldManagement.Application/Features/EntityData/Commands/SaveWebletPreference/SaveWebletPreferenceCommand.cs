using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.FieldManagement.Application.Features.EntityData.Commands.SaveWebletPreference;

public class SaveWebletPreferenceCommand : IRequest<int>
{
    public Guid UserSyncId { get; set; }
    public string PreferenceType { get; set; }
    public string PreferenceJsonValue { get; set; }
}
