using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.RegisterAssets
{
    public class AssetRegisterCommand : AssetRegisterDTO, IRequest<string>
    {
    }
}
