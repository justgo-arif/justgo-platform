using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Booking.Application.Features.BookingClasses.Commands.UpdatePricingChartDiscountStatus
{
    public class UpdatePricingChartDiscountStatusCommand : IRequest<int>
    {
        public int PricingChartDiscountId { get; set; }
        public int PricingChartDiscountStatus { get; set; }
    }
}
