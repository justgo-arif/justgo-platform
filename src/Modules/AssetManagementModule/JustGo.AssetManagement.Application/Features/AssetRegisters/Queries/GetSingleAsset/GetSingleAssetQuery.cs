using JustGo.AssetManagement.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetSingleAsset
{
    public class GetSingleAssetQuery : IRequest<AssetDTO>
    {
        
        public string AssetRegisterId { get; set; }

    }
}
  