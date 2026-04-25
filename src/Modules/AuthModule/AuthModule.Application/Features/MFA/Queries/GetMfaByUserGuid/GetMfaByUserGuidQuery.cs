using AuthModule.Domain.Entities.MFA;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.MFA.Queries.GetMfaByUserGuid;

public class GetMfaByUserGuidQuery : IRequest<UserMFA>
{
    public Guid Id { get; set; }
    public GetMfaByUserGuidQuery(Guid id)
    {
        Id = id;
    }
}
