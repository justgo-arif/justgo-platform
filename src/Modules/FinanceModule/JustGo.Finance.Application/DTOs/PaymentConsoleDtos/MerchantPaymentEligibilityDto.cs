using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Finance.Application.DTOs.PaymentConsoleDtos;

public class MerchantPaymentEligibilityDto
{
    public int MerchantId { get; set; }
    public bool IsEligible { get; set; }
    public string Reason { get; set; } = string.Empty; // optional: explain why not eligible
}
