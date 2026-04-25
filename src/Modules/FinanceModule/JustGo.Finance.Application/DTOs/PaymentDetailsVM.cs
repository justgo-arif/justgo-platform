namespace JustGo.Finance.Application.DTOs
{
    public class PaymentDetailsVM
    {
        public Guid PaymentGuid { get; set; }
        public PaymentCardInfo? PaymentCardInfo { get; set; }
        public PaymentBreakdown? PaymentBreakdown { get; set; }
    }

}
