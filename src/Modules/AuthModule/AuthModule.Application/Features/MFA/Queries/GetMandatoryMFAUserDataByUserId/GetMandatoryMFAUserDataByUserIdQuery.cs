using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.MFA.Queries.GetMandatoryMFAUserDataByUserId;

public class GetMandatoryMFAUserDataQuery : IRequest<bool>
{
    public int MemberDocId { get; set; }
    public GetMandatoryMFAUserDataQuery(int id)
    {
        MemberDocId = id;
    }
}
