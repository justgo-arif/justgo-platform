using AuthModule.Domain.Entities.MFA;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.MFA.Commands.Create;

public class SaveBackupCodeCommandHandler : IRequestHandler<SaveBackupCodeCommand, bool>
{
    private readonly LazyService<IWriteRepository<UserMFA>> _writeRepository;
    private readonly IUtilityService _utilityService;

    public SaveBackupCodeCommandHandler(LazyService<IWriteRepository<UserMFA>> writeRepository, IUtilityService utilityService)
    {
        _writeRepository = writeRepository;
        _utilityService = utilityService;
    }

    public async Task<bool> Handle(SaveBackupCodeCommand request, CancellationToken cancellationToken)
    {
        string sql = @"if not exists(select top 1 UserId from UserMFA where UserId = @UserId)
                        begin
                        	insert into UserMFA (UserId,BackUpCode)
                        	select Userid, @BackupCode from [User]  where Userid = @UserId
                        end
                        else
                        begin
                         update UserMFA set BackUpCode = @BackupCode where UserId = @UserId	
                        end";

        var queryParameters = new DynamicParameters();
        queryParameters.Add("@UserId", request.UserId);
        queryParameters.Add("@BackupCode", _utilityService.EncryptData(request.BackupCode));

        var result = await _writeRepository.Value.ExecuteAsync(sql, queryParameters, null, "text");

        return result >= 0;
    }
}
