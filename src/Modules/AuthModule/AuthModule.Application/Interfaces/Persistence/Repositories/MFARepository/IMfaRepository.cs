using AuthModule.Application.DTOs.MFA;
using AuthModule.Domain.Entities.MFA;

namespace AuthModule.Application.Interfaces.Persistence.Repositories.MFARepository;

public interface IMfaRepository
{
    string GenerateBackupCode();
    Task<MFASetupResponseDto> SetupAuthenticator(string authChannel, IDictionary<string, string> args);
    Task<MFASetupResponseDto> GetAuthenticatorKey(string authChannel, IDictionary<string, string> args);
    Task<MFAVerifyDto> VerifyCode(string authChannel, IDictionary<string, string> args, UserMFA userMFA);
}
