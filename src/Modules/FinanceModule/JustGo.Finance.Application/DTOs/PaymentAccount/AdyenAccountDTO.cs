using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Finance.Application.DTOs.PaymentAccount;

public class AdyenAccountDTO
{
    public string? LegalEntityId { get; set; }
    public string? BusinessLineId { get; set; }
    public string? StoreId { get; set; }
    public string? PayoutSchedule { get; set; }
    public string? SweepId { get; set; }
    public string? StatementDescriptor { get; set; }
}
