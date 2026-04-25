using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetLicenses.Queries.GetAssetLicenseById
{
    public class GetAssetLicenseByIdQuery : IRequest<List<AssetLicenseResultDTO>>
    {
        public string AssetRegisterId { get; set; }
        public GetAssetLicenseByIdQuery(string assetRegisterId)
        {
            AssetRegisterId = assetRegisterId;
        }
    }
}