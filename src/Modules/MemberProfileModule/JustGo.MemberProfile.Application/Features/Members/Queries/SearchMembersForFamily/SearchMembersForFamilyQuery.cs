using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.SearchMembersForFamily;

public class SearchMembersForFamilyQuery : IRequest<List<FindMemberDto>>
{
    public required string Email { get; set; }
    public string? MID { get; set; } // Optional Member ID filter
    public DateTime? DateOfBirth { get; set; } // Optional Date of Birth filter
}
