using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetStatusCommands
{
    public class ChangeAssetLeaseStatusCommand : IRequest<string>
    {
        public string AssetLeaseId { get; set; }
        public LeaseStatusType  Status { get; set; }
        public RejectionReason? RejectionReason { get; set; }
        public string RejectionNote { get; set; }
    }
}
