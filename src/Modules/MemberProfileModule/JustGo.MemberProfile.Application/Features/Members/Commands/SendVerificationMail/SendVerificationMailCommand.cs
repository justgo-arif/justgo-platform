using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;

namespace JustGo.MemberProfile.Application.Features.Members.Commands.SendVerificationMail;

public class SendVerificationMailCommand : IRequest<OperationResultDto>
{
    public required Guid UserSyncId { get; set; }
    public required string Type { get; set; }
    //ParentalApproval
    //TwoFactor
}

