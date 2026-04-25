using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.MemberProfile.Application.Features.Preferences.Commands.SaveOptinCurrent
{
    public class SaveOptinCurrentCommand : IRequest<string>
    {
        public required int EntityId { get; set; }
        public int OptinId { get; set; }
        public int ActionUserId { get; set; }
        public bool? OptinValue { get; set; }
    }
}
