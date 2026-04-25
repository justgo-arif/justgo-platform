using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.MFA.Commands.Create;

public class SaveMFALogCommand : IRequest<bool>
{
    public int UserId { get; set; }
    public string Type { get; set; }
    public string Action { get; set; }
    public dynamic Args { get; set; }
    public dynamic Obj { get; set; }

}
