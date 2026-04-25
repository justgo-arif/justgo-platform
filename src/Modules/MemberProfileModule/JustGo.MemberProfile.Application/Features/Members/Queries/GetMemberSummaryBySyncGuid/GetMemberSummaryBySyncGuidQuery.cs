using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberSummaryBySyncGuid;

public class GetMemberSummaryBySyncGuidQuery : IRequest<MemberSummaryDto?>
{
    public Guid Id { get; set; }
    public GetMemberSummaryBySyncGuidQuery(Guid id)
    {
        Id = id;
    }
}
