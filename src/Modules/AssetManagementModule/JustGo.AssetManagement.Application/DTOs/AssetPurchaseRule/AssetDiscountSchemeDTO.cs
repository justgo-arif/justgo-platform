using System;

namespace JustGo.AssetManagement.Application.DTOs.AssetPurchaseRule
{
    public class AssetDiscountSchemeDTO
    {
        public int SchemeId { get; set; }
        public string SchemeName { get; set; } = string.Empty;
        public int OwnerId { get; set; }
        public int? AssetTypeId { get; set; }
        public string DiscountType { get; set; } = string.Empty; 
        public decimal DiscountValue { get; set; }
        public string? RuleConfig { get; set; } 
        public bool IsActive { get; set; }
    }
}