using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetStatusCommands
{
    public class ChangeAssetCredentialStatusCommand : IRequest<string>
    {
        public string AssetCredentialId { get; set; }
        public CredentialStatusType  Status { get; set; }
    }
}
