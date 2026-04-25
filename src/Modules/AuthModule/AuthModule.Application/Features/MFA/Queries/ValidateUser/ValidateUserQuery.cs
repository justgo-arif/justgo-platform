using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.MFA.Queries.ValidateUser;

public class ValidateUserQuery : IRequest<bool>
{
    public string UserName { get; set; }
    public string Password { get; set; }

}
