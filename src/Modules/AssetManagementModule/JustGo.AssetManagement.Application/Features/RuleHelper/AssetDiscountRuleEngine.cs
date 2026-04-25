using JustGo.AssetManagement.Application.DTOs.AssetPurchaseRule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.RuleHelper
{
    public static class AssetDiscountRuleEngine
    {

        public static AssetGroupDiscountResultDTO CalculateCartDiscount(List<CartItem> cart, List<AssetPurchaseLicenseGroup> groups, List<AssetPurchaseDiscountRule> rules)
        {
            decimal total = 0m;
            List<CartItem> cartItems = new List<CartItem>();

            foreach (var rule in rules)
            {
                var group = groups.FirstOrDefault(g => g.GroupId == rule.GroupId);
                if (group == null) continue;

                var groupItems = cart.Where(c => group.LicenseDocIds.Contains(c.LicenseDocId)).ToList();

                if (!groupItems.Any())
                    continue;

                int totalQuantity = groupItems.Count();
                decimal normalPrice = groupItems.Sum(i => i.Quantity * i.UnitPrice);

                if (totalQuantity >= rule.MinQuantity && totalQuantity <= rule.MaxQuantity)
                {
                    if (rule.DiscountType == "FixedPrice")
                    {
                        //total += rule.Value;
                        total = rule.Value;
                        cartItems.AddRange(groupItems);
                    }
                    else if (rule.DiscountType == "Percentage")
                    {
                        var discountAmount = (normalPrice * rule.Value) / 100m;
                        //total += normalPrice - discountAmount;
                        total = normalPrice - discountAmount;
                    }

                    foreach (var item in groupItems) cart.Remove(item);
                }
            }

            return new AssetGroupDiscountResultDTO() { Amount = total,CartItems = cartItems };
        }
    }
}
