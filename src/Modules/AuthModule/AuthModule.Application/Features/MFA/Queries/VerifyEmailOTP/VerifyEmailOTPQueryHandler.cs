using AuthModule.Application.DTOs.MFA;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.MFA.Queries.GetMfaByUserId;

public class VerifyEmailOTPQueryHandler : IRequestHandler<VerifyEmailOTPQuery, MFAVerifyDto>
{
    private readonly LazyService<IReadRepository<string>> _readRepository;
    private readonly LazyService<IWriteRepository<string>> _writeRepository;

    public VerifyEmailOTPQueryHandler(LazyService<IReadRepository<string>> readRepository, LazyService<IWriteRepository<string>> writeRepository)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
    }

    public async Task<MFAVerifyDto> Handle(VerifyEmailOTPQuery request, CancellationToken cancellationToken)
    {
        string sql = @"SELECT 
                        CASE 
                            WHEN COUNT(*) = 0 THEN 0
                            ELSE 
                                CASE 
                                    WHEN DATEDIFF(MINUTE, MAX(CreatedDate), GETDATE()) < 6 THEN 1 
                                    ELSE 0 
                                END
                        END AS IsValid
                    FROM MFAOtp 
                    WHERE IsUsed = 0 AND UserId = @UserId AND OTPCode = @OtpCode";


        var queryParameters = new DynamicParameters();
        queryParameters.Add("@UserId", request.UserId);
        queryParameters.Add("@OtpCode", request.OTPCode);

        var result = await _readRepository.Value.GetAsync(sql, queryParameters, null, "text");
        if (Convert.ToInt32(result) > 0) await UpdateUsedCode(request.UserId);//update status for used code

        return new MFAVerifyDto { IsValid = Convert.ToInt32(result) > 0, Message = Convert.ToInt32(result) > 0 ? "Verification successfull." : "Verification failed." };
    }
    private async Task UpdateUsedCode(int UserId)
    {
        string sql = @"UPDATE MFAOtp SET IsUsed = 1 WHERE UserId = @UserId AND IsUsed = 0";

        var queryParameters = new DynamicParameters();
        queryParameters.Add("@UserId", UserId);

        await _writeRepository.Value.ExecuteAsync(sql, queryParameters, null, "text");
    }

}
