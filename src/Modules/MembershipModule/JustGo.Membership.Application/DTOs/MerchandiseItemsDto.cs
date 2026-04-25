namespace JustGo.Membership.Application.DTOs
{
    public class MerchandiseItemsDto
    {
        public string Category { get; set; }= string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ClubName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string ProductImage { get; set; } = string.Empty;
    }
}

