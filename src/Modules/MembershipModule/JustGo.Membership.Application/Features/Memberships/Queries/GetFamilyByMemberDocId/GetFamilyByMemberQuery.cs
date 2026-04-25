using JustGo.Membership.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetFamilyByMemberDocId
{
    public class GetFamilyByMemberQuery : IRequest<FamilyDetailsDto>
    {
        public int MemberDocId { get; set; }

        public GetFamilyByMemberQuery(int memberDocId)
        {
            MemberDocId = memberDocId;
        }
    }
}
