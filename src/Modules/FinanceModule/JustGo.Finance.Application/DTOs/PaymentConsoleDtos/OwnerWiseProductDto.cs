namespace JustGo.Finance.Application.DTOs.PaymentConsoleDtos
{
    public class OwnerWiseProductDto
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }
        public int OwnerId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
}
