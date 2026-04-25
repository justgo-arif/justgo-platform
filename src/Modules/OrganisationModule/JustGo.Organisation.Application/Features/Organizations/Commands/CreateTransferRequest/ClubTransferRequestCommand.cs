using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;

namespace JustGo.Organisation.Application.Features.Organizations.Commands.ClubTransferRequest;

public class ClubTransferRequestCommand : IRequest<OperationResultDto<int>>
{
    public required Guid MemberSyncGuid { get; set; }
    public required Guid FromClubSyncGuid { get; set; }
    public required Guid ToClubSyncGuid { get; set; }
    public required string ReasonForMove { get; set; }
}
