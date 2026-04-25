using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.ChangeAssetStatusCommands
{
    public class ChangeAssetLicenseStatusCommand : IRequest<string>
    {
        public string AssetLicenseId { get; set; }
        public LicenseStatusType  Status { get; set; }
    }
}
