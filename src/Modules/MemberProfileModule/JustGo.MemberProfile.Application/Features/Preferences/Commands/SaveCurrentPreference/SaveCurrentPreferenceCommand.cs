using Json.Schema;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.ComponentModel.DataAnnotations;

namespace JustGo.MemberProfile.Application.Features.Preferences.Commands.SaveCurrentPreference;

public class SaveCurrentPreferenceCommand : IRequest<string>
{
    public required Guid MemberSyncGuid { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "OptinId must be greater than 0.")]
    public required int OptinId { get; set; }

    public required Guid ActionUserSyncGuid { get; set; }
    public required bool OptinValue { get; set; }
}
