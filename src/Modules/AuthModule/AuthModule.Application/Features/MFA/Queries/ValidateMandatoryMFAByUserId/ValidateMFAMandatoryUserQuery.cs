using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.MFA.Queries.ValidateMandatoryMFAByUserId;

public class ValidateMFAMandatoryUserQuery : IRequest<bool>
{
    public int UserId { get; set; }
    public ValidateMFAMandatoryUserQuery(int id)
    {
        UserId = id;
    }
}
