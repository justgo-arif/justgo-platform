namespace JustGo.Finance.Application.DTOs
{
    public class PaymentSummaryVM
    {
        public Guid PaymentId { get; set; }
        public PaymentSummary? PaymentSummary { get; set; }
        public Address? BillingDetails { get; set; }
        public Address? ShippingDetails { get; set; }
    }
}
