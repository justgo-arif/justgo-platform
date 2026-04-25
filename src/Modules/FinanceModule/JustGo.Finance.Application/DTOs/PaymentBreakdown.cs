namespace JustGo.Finance.Application.DTOs
{
    public class PaymentBreakdown
    {
        public decimal PaymentAmount { get; set; }
        public decimal PaymentFee { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal NetAmount { get; set; }
    }
}
