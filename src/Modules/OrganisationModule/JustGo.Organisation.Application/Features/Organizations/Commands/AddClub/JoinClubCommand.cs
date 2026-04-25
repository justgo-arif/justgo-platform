using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;
using System.ComponentModel;

namespace JustGo.Organisation.Application.Features.Organizations.Commands.AddClub;

public class JoinClubCommand : IRequest<OperationResultDto>
{
    public required Guid ClubGuid { get; set; }
    public required Guid MemberGuid { get; set; }
    [DefaultValue("Member")]
    public string ClubMemberRoles { get; set; } = "Member";
}

