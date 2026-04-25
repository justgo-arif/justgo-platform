namespace JustGo.Finance.Application.DTOs.SubscriptionDTOs
{
    public class SubscriptionResponse
    {
        public int RecurringPaymentCustomerId { get; set; }
        public string? PlanName { get; set; }
        public string? Status { get; set; }
        public string? Currency { get; set; } 
        public string? BillingCycle { get; set; }
        public string? NextPaymentDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public Address? BillingDetails { get; set; }
        public int? PricingMode { get; set; }
    }
}
