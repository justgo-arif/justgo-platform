namespace JustGo.Finance.Application.DTOs.ExportDTOs
{
    public class ExportedReceiptDto
    {
        public required string PaymentId { get; set; }
        public required string CustomerName { get; set; }
        public required string CustomerId { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? PaymentType { get; set; }
        public string? ReceiptStatus { get; set; }
        public required string PaymentDate { get; set; }
        public string? Products { get; set; }
    }

}
