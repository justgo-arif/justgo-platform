using AuthModule.Application.DTOs.MFA;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.MFA.Commands.Create;

public class SetupEmailAuthenticatorCommand : IRequest<MFASetupResponseDto>
{
    public int UserId { get; set; }
    public string Email { get; set; }
}
