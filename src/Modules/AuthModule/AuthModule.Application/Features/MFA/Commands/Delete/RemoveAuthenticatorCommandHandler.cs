using AuthModule.Domain.Entities.MFA;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.MFA.Commands.Delete;

public class RemoveAuthenticatorCommandHandler : IRequestHandler<RemoveAuthenticatorCommand, bool>
{
    private readonly LazyService<IWriteRepository<UserMFA>> _writeRepository;

    public RemoveAuthenticatorCommandHandler(LazyService<IWriteRepository<UserMFA>> writeRepository)
    {
        _writeRepository = writeRepository;
    }

    public async Task<bool> Handle(RemoveAuthenticatorCommand request, CancellationToken cancellationToken)
    {
        string sql = "";
        var channelMappings = new Dictionary<string, (string Sql, string Category)>
                {
                    { "whatsapp", (
                        "UPDATE UserMFA SET EnableWhatsapp = null, EnableWhatsappDate = null, WhatsAppState = 2, WhatsAppNumber = null, PhoneCode = null, CountryCode = null WHERE UserId = @UserId",
                        "WhatsApp"
                    )},
                    { "authapp", (
                        "UPDATE UserMFA SET EnableAuthenticatorApp = null, EnableAuthenticatorAppDate = null, AuthenticatorAppState = 2 WHERE UserId = @UserId",
                        "Authenticator App"
                    )},
                     { "emailauth", (
                        "UPDATE UserMFA SET IsEmailAuthEnabled = null, Email = null, EmailAuthEnableDate = null, EmailAuthState = 2 where UserId = @UserId",
                        "Email Auth"
                    )}
                };

        if (channelMappings.TryGetValue(request.AuthChannel.ToLower(), out var mapping))
        {
            sql = mapping.Sql;
        }

        var queryParameters = new DynamicParameters();
        queryParameters.Add("@UserId", request.UserId);

        var result = await _writeRepository.Value.ExecuteAsync(sql, queryParameters, null, "text");

        return result >= 0;
    }
}
