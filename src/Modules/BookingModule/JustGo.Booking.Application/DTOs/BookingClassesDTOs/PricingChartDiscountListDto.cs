namespace JustGo.Booking.Application.DTOs.BookingClassesDTOs
{
    public class PricingChartDiscountListDto
    {
        public int PricingChartDiscountId { get; set; }
        public string PricingChartDiscountName { get; set; } = string.Empty;
        public int PricingChartDiscountType { get; set; }
        public decimal PricingChartDiscountValue { get; set; }
        public int PricingChartDiscountStatus { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public int PricingChartId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<int> PricingChartIds { get; set; } = new();
        public List<string> PricingChartNames { get; set; } = new();
    }
}
