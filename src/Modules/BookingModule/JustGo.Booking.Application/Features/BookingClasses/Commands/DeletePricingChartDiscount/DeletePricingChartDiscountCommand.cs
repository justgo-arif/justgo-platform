using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Booking.Application.Features.BookingClasses.Commands.DeletePricingChartDiscount
{
    public class DeletePricingChartDiscountCommand : IRequest<int>
    {
        public DeletePricingChartDiscountCommand(int pricingChartDiscountId)
        {
            PricingChartDiscountId = pricingChartDiscountId;
        }

        public int PricingChartDiscountId { get; set; }
    }
}
