namespace JustGo.Finance.Application.DTOs
{
    public class PaymentInfoDto
    {
        public string Id { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmailAddress { get; set; } = string.Empty;
        public string ProfilePicURL { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public decimal? TotalAmount { get; set; }
        public decimal? TransactionFee { get; set; }
        public string? PaymentDate { get; set; }
        public int StatusId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ReceiptStatus { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
