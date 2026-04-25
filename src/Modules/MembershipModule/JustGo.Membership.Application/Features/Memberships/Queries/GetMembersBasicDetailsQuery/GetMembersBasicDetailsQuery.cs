using JustGo.Membership.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetMembersBasicDetailsQuery
{
    public class GetMembersBasicDetailsQuery : IRequest<List<MemberDetailsDto>>
    {
        public List<int> MemberDocIds { get; set; }

        public GetMembersBasicDetailsQuery(List<int> memberDocIds)
        {
            MemberDocIds = memberDocIds;
        }
    }
}
