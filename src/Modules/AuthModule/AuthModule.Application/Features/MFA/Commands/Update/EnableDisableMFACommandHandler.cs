using AuthModule.Domain.Entities.MFA;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.MFA.Commands.Update;

public class EnableDisableMFACommandHandler : IRequestHandler<EnableDisableMFACommand, bool>
{
    private readonly LazyService<IWriteRepository<UserMFA>> _writeRepository;

    public EnableDisableMFACommandHandler(LazyService<IWriteRepository<UserMFA>> writeRepository)
    {
        _writeRepository = writeRepository;
    }

    public async Task<bool> Handle(EnableDisableMFACommand request, CancellationToken cancellationToken)
    {
        string sql = "";
        string category = "";
        var channelMappings = new Dictionary<string, (string Sql, string Category, string StateColumn)>
            {
                { "whatsapp", ("UPDATE UserMFA SET EnableWhatsapp = @updateFlag, WhatsAppState = 1 WHERE UserId = @UserId", "WhatsApp", "WhatsAppState") },
                { "authapp", ("UPDATE UserMFA SET EnableAuthenticatorApp = @updateFlag, AuthenticatorAppState = 1,EnableAuthenticatorAppDate=GETUTCDATE() WHERE UserId = @UserId", "Authenticator App", "AuthenticatorAppState") },
                { "emailauth", ("UPDATE UserMFA SET IsEmailAuthEnabled = @updateFlag, EmailAuthState = 1,EmailAuthEnableDate=GETUTCDATE() WHERE UserId = @UserId", "Email Auth", "EmailAppState") }
            };

        if (channelMappings.TryGetValue(request.AuthChannel.ToLower(), out var mapping))
        {
            sql = mapping.Sql;
            category = mapping.Category;
        }

        var queryParameters = new DynamicParameters();
        queryParameters.Add("@updateFlag", request.UpdateFlag);
        queryParameters.Add("@UserId", request.UserId);

        var result = await _writeRepository.Value.ExecuteAsync(sql, queryParameters, null, "text");

        return result >= 0;
    }
}
