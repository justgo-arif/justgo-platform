using JustGo.AssetManagement.Application.DTOs.AssetPurchaseRule;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using System.Threading;

namespace JustGo.AssetManagement.Application.Features.AssetPurchaseRules.Queries.GetAssetPurchaseDiscountConfig
{
    public class GetAssetPurchaseDiscountConfigQuery : IRequest<AssetDiscountSchemeDTO?>
    {
        public int OwnerId { get; set; }
        public GetAssetPurchaseDiscountConfigQuery(int ownerId)
        {
            OwnerId = ownerId;
        }
    }
}