namespace JustGo.Finance.Application.DTOs
{
    public class PaymentOverviewDto
    {
        public string PaymentId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerEmailAddress { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentDate { get; set; } = string.Empty;
        public string Paymentpaidtime { get; set; } = string.Empty;
        public string ReceiptStatus { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public string ProfilePicURL { get; set; } = string.Empty;
        public string? Currency { get; set; } 
        public decimal TotalAmount { get; set; }    
        public decimal RefundedAmount { get; set; }
        public bool IsRefundable { get; set; }
    }

}
