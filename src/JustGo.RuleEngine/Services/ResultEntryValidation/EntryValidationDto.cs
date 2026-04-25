using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.RuleEngine.Services.ResultEntryValidation
{
    public class EntryValidationDto
    {
        public string ItemDisplayName { get; set; } = string.Empty;
        public string ValidationStatus { get; set; } = string.Empty;
        public string ErrorReason { get; set; } = string.Empty;
        public string ItemValue { get; set; } = string.Empty;
        public bool IsValidItem { get; set; }
    }
}
