using AuthModule.Domain.Entities.MFA;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.MFA.Commands.Update;

public class EnableDisableMFAForAdminCommandHandler : IRequestHandler<EnableDisableMFAForAdminCommand, bool>
{
    private readonly LazyService<IWriteRepository<UserMFA>> _writeRepository;

    public EnableDisableMFAForAdminCommandHandler(LazyService<IWriteRepository<UserMFA>> writeRepository)
    {
        _writeRepository = writeRepository;
    }

    public async Task<bool> Handle(EnableDisableMFAForAdminCommand request, CancellationToken cancellationToken)
    {
        string sql = @"UPDATE MFA SET EnableAuthenticatorApp = @appUpdateFlag , EnableWhatsapp = @whatsAppUpdateFlag, IsEmailAuthEnabled = @emailAuthFlag   from [UserMFA] MFA inner join [User] U on U.Userid = MFA.UserId where U.MemberDocId = @MemberDocId";

        var queryParameters = new DynamicParameters();
        queryParameters.Add("@MemberDocId", request.MemberDocId);
        queryParameters.Add("@appUpdateFlag", request.AppUpdateFlag);
        queryParameters.Add("@whatsAppUpdateFlag", request.WhatsAppUpdateFlag);
        queryParameters.Add("@emailAuthFlag", request.EmailAuthFlag);

        var result = await _writeRepository.Value.ExecuteAsync(sql, queryParameters, null, "text");

        return result >= 0;
    }
}
