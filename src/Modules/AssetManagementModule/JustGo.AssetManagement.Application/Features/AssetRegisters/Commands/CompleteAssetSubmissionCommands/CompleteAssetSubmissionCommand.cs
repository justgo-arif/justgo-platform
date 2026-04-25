using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.CompleteAssetSubmissionCommands
{
    public class CompleteAssetSubmissionCommand: IRequest<bool>
    {
        public string AssetRegisterId { get; set; }
    }
}
