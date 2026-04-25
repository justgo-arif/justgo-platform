using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetStatusCommands
{
    public class ChangeAssetTransferStatusCommand : IRequest<string>
    {
        public string AssetTransferId { get; set; }
        public TransferStatusType  Status { get; set; }
        public RejectionReason? RejectionReason { get; set; }
        public string RejectionNote { get; set; }
    }
}
