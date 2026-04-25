using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Finance.Domain.Entities;

public class AdyenApiKeySettings
{
    public string Environment { get; set; } = string.Empty;
    public string CheckoutApiKey { get; set; } = string.Empty;
    public string BalancePlatformApiKey { get; set; } = string.Empty;
    public string LegalEntityApiKey { get; set; } = string.Empty;
    public string MerchantCode { get; set; } = string.Empty;
    public string HmacKey { get; set; } = string.Empty;
    public string BalancePlatformTransferHmacKey { get; set; } = string.Empty;
    public string BalancePlatformReportHmacKey { get; set; } = string.Empty;
    public string EnpointPrefix { get; set; } = string.Empty;
}
