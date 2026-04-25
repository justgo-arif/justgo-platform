namespace JustGo.Finance.Application.DTOs.PaymentConsoleDtos
{
    public class MerchantPaymentVisibilityDto
    {
        public Guid MerchantId { get; set; }
        public bool IsPaymentEligible { get; set; } = false;
        public bool IsMerchantPaymentEligible { get; set; } = false;
        public string Reason { get; set; } = string.Empty;
    }
}
