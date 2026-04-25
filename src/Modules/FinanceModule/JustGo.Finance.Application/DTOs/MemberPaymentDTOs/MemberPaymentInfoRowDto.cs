namespace JustGo.Finance.Application.DTOs.MemberPaymentDTOs
{
    public class MemberPaymentInfoRowDto
    {
        public int DocId { get; set; }
        public int ProductDocId { get; set; } 
        public string ProductName { get; set; } = string.Empty;
        public string ProductReference { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Gross { get; set; }
        public string ProductImageURL { get; set; } = string.Empty;
    }
}
