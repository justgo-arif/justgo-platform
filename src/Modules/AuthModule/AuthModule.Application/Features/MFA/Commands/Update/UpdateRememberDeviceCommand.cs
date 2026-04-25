using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.MFA.Commands.Update;

public class UpdateRememberDeviceCommand : IRequest<bool>
{
    public int UserId { get; set; }
    public bool IsAdmin { get; set; } = false;
}
