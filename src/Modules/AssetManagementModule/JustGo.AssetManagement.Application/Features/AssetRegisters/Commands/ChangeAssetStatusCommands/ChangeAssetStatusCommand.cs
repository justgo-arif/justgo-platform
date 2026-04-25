using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetStatusCommands
{
    public class ChangeAssetStatusCommand: IRequest<string>
    {
        public string AssetRegisterId { get; set; }
        public AssetStatusType  Status { get; set; }
    }
}
