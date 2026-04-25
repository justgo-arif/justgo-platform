using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Finance.Application.DTOs
{
    public class AdyenBalanceAccountDTO
    {
        public string BalanceAccountId { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? SweepId { get; set; }
        public string? PayoutSchedule { get; set; }
        public string? AccountHolderId { get; set; }
    }
}
