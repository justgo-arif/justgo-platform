using JustGo.AssetManagement.Application.DTOs.AssetTransfers;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetOwnershipTransfers.Commands.CreateAssetTransfers
{
    public class CreateAssetTransferCommand : AssetTransferDTO, IRequest<string>
    {
    }
}
