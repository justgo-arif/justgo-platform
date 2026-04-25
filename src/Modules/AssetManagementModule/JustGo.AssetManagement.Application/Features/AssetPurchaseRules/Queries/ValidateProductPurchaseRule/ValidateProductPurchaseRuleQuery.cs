using JustGo.AssetManagement.Application.DTOs.AssetPurchaseRule;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.AssetManagement.Application.Features.AssetPurchaseRules.Queries.ValidateProductPurchaseRule
{
    public class ValidateProductPurchaseRuleQuery : IRequest<AssetPurchaseRuleResultDTO>
    {
        public Guid ProductId { get; set; }
        public Guid AssetRegisterId { get; set; }

        public ValidateProductPurchaseRuleQuery(Guid productId, Guid assetRegisterId)
        {
            ProductId = productId;
            AssetRegisterId = assetRegisterId;
        }
    }
}