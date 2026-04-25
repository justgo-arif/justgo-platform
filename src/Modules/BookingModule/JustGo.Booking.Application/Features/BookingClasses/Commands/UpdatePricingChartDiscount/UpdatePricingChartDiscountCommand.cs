using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Booking.Application.Features.BookingClasses.Commands.UpdatePricingChartDiscount
{
    public class UpdatePricingChartDiscountCommand : IRequest<int>
    {
        public int PricingChartDiscountId { get; set; }
        public string PricingChartDiscountName { get; set; } = string.Empty;
        public int PricingChartDiscountType { get; set; }
        public decimal PricingChartDiscountValue { get; set; }
        public List<int> PricingChartIds { get; set; } = new List<int>();
    }
}
