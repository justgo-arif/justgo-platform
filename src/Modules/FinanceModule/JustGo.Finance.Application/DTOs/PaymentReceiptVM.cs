namespace JustGo.Finance.Application.DTOs
{
    public class PaymentReceiptVM
    {
        public int? TotalCount { get; set; }
        public int PageNo { get; set; }
        public int Size { get; set; }
        public List<Product>? Products { get; set; }
    }
}
