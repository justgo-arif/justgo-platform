namespace JustGo.Finance.Application.DTOs.InstallmentDTOs
{
    public class RecurringPaymentHistory
    {
        public string? InvoiceId { get; set; }
        public int PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? BillingDate { get; set; }
    }
}
