using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.MFA.Queries.ValidateMFAUserBackupCode
{
    public class ValidateMFAUserBackupCodeQueryHandler : IRequestHandler<ValidateMFAUserBackupCodeQuery, bool>
    {
        private readonly LazyService<IReadRepository<string>> _readRepository;
        private readonly IUtilityService _utilityService;

        public ValidateMFAUserBackupCodeQueryHandler(LazyService<IReadRepository<string>> readRepository, IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _utilityService = utilityService;
        }

        public async Task<bool> Handle(ValidateMFAUserBackupCodeQuery request, CancellationToken cancellationToken)
        {
            string sql = @"select top 1 LoginId from UserMFA mfa inner join [user] u on u.Userid=mfa.UserId where u.LoginId = @UserName and mfa.BackUpCode = @BackUpCode";


            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserName", request.UserName);
            queryParameters.Add("@BackUpCode", _utilityService.EncryptData(request.BackupCode));

            var result = await _readRepository.Value.GetAsync(sql, null, null, "text");

            return !string.IsNullOrEmpty(result);
        }
    }
}
