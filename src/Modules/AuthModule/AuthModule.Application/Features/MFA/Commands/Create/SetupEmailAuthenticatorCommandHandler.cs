using AuthModule.Application.DTOs.MFA;
using AuthModule.Application.EmailServices;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.MFA.Commands.Create;

public class SetupEmailAuthenticatorCommandHandler : IRequestHandler<SetupEmailAuthenticatorCommand, MFASetupResponseDto>
{
    private readonly LazyService<IWriteRepository<object>> _writeRepository;
    private readonly LazyService<EmailService> _emailService;
    public SetupEmailAuthenticatorCommandHandler(LazyService<IWriteRepository<object>> writeRepository
        , LazyService<EmailService> emailService)
    {
        _writeRepository = writeRepository;
        _emailService = emailService;
    }

    public async Task<MFASetupResponseDto> Handle(SetupEmailAuthenticatorCommand request, CancellationToken cancellationToken)
    {
        var DtoModel = new MFASetupResponseDto();
        Random random = new Random();
        string otp = random.Next(100000, 999999).ToString();
        //send Email
        //execute sp SEND_EMAIL_BY_SCHEME start
        var queryParametersSendEmail = new DynamicParameters();
        queryParametersSendEmail.Add("@ForEntityId", request.UserId);
        queryParametersSendEmail.Add("@MessageScheme", "Account/OTP Verification");
        queryParametersSendEmail.Add("@Argument", otp);
        queryParametersSendEmail.Add("@TypeEntityId", -1);
        queryParametersSendEmail.Add("@InvokeUserId", -1);
        queryParametersSendEmail.Add("@OwnerId", 0);
        queryParametersSendEmail.Add("@OwnerType", "NGB");
        queryParametersSendEmail.Add("@TestEmailAddress", "N/A");
        queryParametersSendEmail.Add("@GetInfo", -1);

        var d = await _writeRepository.Value.ExecuteAsync("SEND_EMAIL_BY_SCHEME", queryParametersSendEmail, null, "sp");
        //execute sp SEND_EMAIL_BY_SCHEME end

        await _emailService.Value.Execute();//send email



        //save otp
        string sql = @"UPDATE MFAOtp SET IsUsed = 1 WHERE UserId = @UserId AND IsUsed = 0
                           INSERT INTO [dbo].[MFAOtp] ([UserId],[OTPCode])
                           VALUES (@UserId, @OTPCode)";

        var queryParameters = new DynamicParameters();
        queryParameters.Add("@UserId", request.UserId);
        queryParameters.Add("@OTPCode", otp);

        var result = await _writeRepository.Value.ExecuteAsync(sql, queryParameters, null, "text");

        if (result >= 0)
        {
            DtoModel = new MFASetupResponseDto
            {
                To = request.Email,
                IsSuccess = true,
                StatusMessage = "Verification code has been sent to your email Successfully",
            };
        }
        return DtoModel;
    }
}
