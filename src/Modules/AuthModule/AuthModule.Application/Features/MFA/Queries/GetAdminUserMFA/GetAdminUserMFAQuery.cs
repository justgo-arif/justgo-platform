using AuthModule.Domain.Entities.MFA;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.MFA.Queries.GetMfaByUserId;

public class GetAdminUserMFAQuery : IRequest<UserMFA>
{
    public int MemberDocId { get; set; }
    public GetAdminUserMFAQuery(int id)
    {
        MemberDocId = id;
    }
}
