using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;

namespace JustGo.Organisation.Application.Features.Organizations.Commands.LeaveClub;

public class LeaveClubCommand : IRequest<OperationResultDto<string>>
{
    public required Guid ClubGuid { get; set; }
    public required Guid MemberGuid { get; set; }
    public required string Reason { get; set; }
    public required string ClubMemberRoles { get; set; }
}
