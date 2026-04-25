using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.MFA.Queries.ValidateMFAUserBackupCode
{
    public class ValidateMFAUserBackupCodeQuery : IRequest<bool>
    {
        public string UserName { get; set; }
        public string BackupCode { get; set; }
    }
}
