using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.AssetPurchaseRule
{
    public class AssetPurchaseRuleResultDTO
    {
        public bool IsEligible { get; set; }
        public string? Reason { get; set; }
    }
}
