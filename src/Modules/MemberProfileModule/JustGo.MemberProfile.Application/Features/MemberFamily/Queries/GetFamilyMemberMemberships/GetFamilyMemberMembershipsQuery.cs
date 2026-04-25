using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Domain.Entities;


namespace JustGo.MemberProfile.Application.Features.MemberFamily.Queries.GetFamilyMemberMemberships
{
    public class GetFamilyMemberMembershipsQuery : IRequest<List<OrganisationType>>
    {
        public GetFamilyMemberMembershipsQuery(Guid id)
        {
            Id = id;
        }
        public Guid Id { get; set; }

    }
}
