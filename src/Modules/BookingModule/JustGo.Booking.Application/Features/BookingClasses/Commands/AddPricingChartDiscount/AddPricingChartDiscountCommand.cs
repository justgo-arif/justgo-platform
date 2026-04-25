using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Booking.Application.Features.BookingClasses.Commands.AddPricingChartDiscount
{
    public class AddPricingChartDiscountCommand : IRequest<int>
    {
        public string PricingChartDiscountName { get; set; } = string.Empty;
        public int PricingChartDiscountType { get; set; }
        public decimal PricingChartDiscountValue { get; set; }
        public int PricingChartDiscountStatus { get; set; }
        public List<int> PricingChartIds { get; set; } = new List<int>();

        //public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
