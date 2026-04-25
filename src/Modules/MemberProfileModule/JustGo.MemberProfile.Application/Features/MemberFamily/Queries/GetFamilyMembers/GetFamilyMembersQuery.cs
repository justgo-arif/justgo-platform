using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Domain.Entities;

namespace JustGo.MemberProfile.Application.Features.MemberFamily.Queries.GetFamilyMembers;

public class GetFamilyMembersQuery : IRequest<List<FamilyMember>>
{
    public Guid Id { get; set; }
    public GetFamilyMembersQuery(Guid id)
    {
        Id = id;
    }
}
