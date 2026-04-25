using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;


namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetSingleAsset
{
    public class GetDuplicateAssetQuery : IRequest<AssetRegister>
    {
        public string AssetRegisterId { get; set; }
        public string AssetName { get; set; }

    }
}
  