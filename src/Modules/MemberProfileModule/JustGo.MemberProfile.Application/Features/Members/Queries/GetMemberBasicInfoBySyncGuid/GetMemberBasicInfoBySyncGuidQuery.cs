using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Domain.Entities;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberBasicInfoBySyncGuid;

public class GetMemberBasicInfoBySyncGuidQuery : IRequest<MemberBasicInfo?>
{
    public GetMemberBasicInfoBySyncGuidQuery(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}
