using AuthModule.Domain.Entities.MFA;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.MFA.Queries.GetMfaByUserId;

public class GetMfaByUserIdQuery : IRequest<UserMFA>
{
    public int UserId { get; set; }
    public GetMfaByUserIdQuery(int id)
    {
        UserId = id;
    }
}
