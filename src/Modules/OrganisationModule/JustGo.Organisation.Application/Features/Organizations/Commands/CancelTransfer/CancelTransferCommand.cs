using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.DTOs;

namespace JustGo.Organisation.Application.Features.Organizations.Commands.CancelTransfer;

public class CancelTransferCommand:IRequest<OperationResultDto>
{
    public CancelTransferCommand(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; }
}
