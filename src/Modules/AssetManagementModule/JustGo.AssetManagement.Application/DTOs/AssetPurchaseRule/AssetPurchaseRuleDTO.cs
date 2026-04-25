using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs.AssetPurchaseRule
{
    public class AssetPurchaseRuleDTO
    {
        public string RuleExpression { get; set; } = string.Empty;
        public string RuleType { get; set; } = string.Empty;
    }
}