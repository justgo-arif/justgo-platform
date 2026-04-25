using JustGo.AssetManagement.Application.DTOs.AssetPurchaseRule;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetPurchaseRules.Queries.EvaluateAssetPurchaseRule
{
    public class EvaluateAssetPurchaseRuleQuery : IRequest<AssetPurchaseRuleResultDTO>
    {
        public EvaluateAssetPurchaseRuleQuery(int productDocId,int userId,int assetid)
        {
            ProductDocId = productDocId;
            UserId = userId;
            AssetId = assetid;
        } 
        public int ProductDocId { get; set; }
        public int UserId { get; set; }
        public int AssetId { get; set; }
    }
}