using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.FindMember;

public class FindMemberQuery : IRequest<FindMemberDto?>
{
    public required string MID { get; set; }
    public string? Email { get; set; }
    public string? LastName { get; set; }
}
