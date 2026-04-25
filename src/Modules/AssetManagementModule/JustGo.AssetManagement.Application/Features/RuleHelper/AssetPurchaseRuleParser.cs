using JustGo.AssetManagement.Application.DTOs.AssetPurchaseRule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.RuleHelper
{
    public static class AssetPurchaseRuleParser
    {
        public static List<ParsedAssetPurchaseRule> Parse(string ruleExpression)
        {
            var rules = new List<ParsedAssetPurchaseRule>();
            if (string.IsNullOrWhiteSpace(ruleExpression))
                return rules;

            // Remove outer parentheses if present
            ruleExpression = ruleExpression.Trim();
            if (ruleExpression.StartsWith("(") && ruleExpression.EndsWith(")"))
                ruleExpression = ruleExpression.Substring(1, ruleExpression.Length - 2);

            // Match RuleName[Expression] patterns, even if separated by &&
            var matches = Regex.Matches(ruleExpression, @"([A-Za-z0-9_]+)\[([^\]]+)\]");
            foreach (Match match in matches)
            {
                var ruleName = match.Groups[1].Value;
                var expr = match.Groups[2].Value;
                rules.Add(new ParsedAssetPurchaseRule
                {
                    RuleName = ruleName,
                    Expression = expr
                });
            }
            return rules;
        }
    }
}
