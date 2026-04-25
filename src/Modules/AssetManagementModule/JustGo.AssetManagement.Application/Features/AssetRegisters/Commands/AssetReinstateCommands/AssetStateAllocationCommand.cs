using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.AssetReinstateCommands
{
    public class AssetStateAllocationCommand : IRequest<bool>
    {
        public string AssetRegisterId { get; set; }
    }
}
