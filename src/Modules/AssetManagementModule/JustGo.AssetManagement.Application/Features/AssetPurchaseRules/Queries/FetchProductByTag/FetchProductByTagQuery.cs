using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetPurchaseRules.Queries.FetchProductByTag
{
    public class FetchProductByTagQuery : IRequest<AssetSurchargeDTOV2>
    {
        public string ProductTag { get; set; } = string.Empty;
        public int OwnerId { get; set; }
    }
}