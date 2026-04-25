using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.MFA.Commands.Create;

public class SaveBackupCodeCommand : IRequest<bool>
{
    public int UserId { get; set; }
    public string BackupCode { get; set; }
}
