using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.EditAssets
{
    public class EditAssetCommand : AssetRegisterDTO, IRequest<string>
    {
        public string AssetRegisterId { get; set; }
    }
}
