using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V3.Classes
{
    public class RuleModel
    {
        public int DocId { get; set; }
        public int RowId { get; set; }
        public List<BaseRule> RuleExpression { get; set; } // Parsed rules
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal Sequence { get; set; }
        public bool IsActive { get; set; }
        public RuleDetails Details { get; set; } // Parsed JSON details
        public string Explanation { get; set; }
    }

    // ✅ Details JSON structure
    public class RuleDetails
    {
        public string Mode { get; set; }
        public string RuleDescription { get; set; }
        public List<RuleGroup> RuleGroups { get; set; }
    }

    public class RuleGroup
    {
        public string GroupName { get; set; }
        public List<object> Rules { get; set; } // Can contain mixed types
    }

    // ✅ Base class for individual rules from RuleExpression
    public abstract class BaseRule
    {
        public string RuleName { get; set; }
    }

    public class GenericGenderRule : BaseRule
    {
        public string Mode { get; set; }
        public string Gender { get; set; }
    }

    public class GenericAgeRule : BaseRule
    {
        public string Mode { get; set; }
        public string Name { get; set; }
  
    }

    public class RawRuleInputDto
    {
        public int DocId { get; set; }
        public int RowId { get; set; }
        public string RuleExpression { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal Sequence { get; set; }
        public bool IsActive { get; set; }
        public string Details { get; set; } // Raw JSON string
        public string Explanation { get; set; }
    }


}
