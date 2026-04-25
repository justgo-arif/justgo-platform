using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;

namespace JustGo.Organisation.Application.Features.Organizations.Commands.SetPrimaryClub;

public class SetPrimaryClubCommand : IRequest<OperationResultDto>
{
    public required Guid MemberSyncGuid { get; set; }
    public required int ClubMemberId { get; set; }

}

