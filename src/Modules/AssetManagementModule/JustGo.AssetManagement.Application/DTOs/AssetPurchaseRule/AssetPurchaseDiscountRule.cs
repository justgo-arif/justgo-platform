using System.Collections.Generic;

namespace JustGo.AssetManagement.Application.DTOs.AssetPurchaseRule
{
    public class AssetPurchaseDiscountRule
    {
        public string GroupId { get; set; } = string.Empty;
        public int MinQuantity { get; set; }
        public int MaxQuantity { get; set; }
        public string DiscountType { get; set; } = string.Empty; // e.g., "FixedPrice", "Percentage"
        public decimal Value { get; set; }
    }

    public class AssetPurchaseLicenseGroup
    {
        public string GroupId { get; set; } = string.Empty;
        public List<int> LicenseDocIds { get; set; } = new List<int>();
    }

    public class AssetPurchaseDiscountConfig
    {
        public List<AssetPurchaseLicenseGroup> LicenseGroups { get; set; }
        public List<AssetPurchaseDiscountRule> DiscountRules { get; set; }
    }

    public class CartItem
    {
        public int LicenseDocId { get; set; }
        public int ProductDocId { get; set; }
        public int OwnerId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}