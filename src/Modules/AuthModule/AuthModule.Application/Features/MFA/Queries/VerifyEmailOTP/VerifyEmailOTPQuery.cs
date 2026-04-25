using AuthModule.Application.DTOs.MFA;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.MFA.Queries.GetMfaByUserId;

public class VerifyEmailOTPQuery : IRequest<MFAVerifyDto>
{
    public int UserId { get; set; }
    public string OTPCode { get; set; }
}
