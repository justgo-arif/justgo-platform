using FluentValidation;

namespace JustGo.Booking.Application.Features.BookingClasses.Commands.UpdatePricingChartDiscountStatus
{
    public class UpdatePricingChartDiscountStatusCommandValidator : AbstractValidator<UpdatePricingChartDiscountStatusCommand>
    {
        public UpdatePricingChartDiscountStatusCommandValidator()
        {
            RuleFor(x => x.PricingChartDiscountId).GreaterThan(0);
            RuleFor(x => x.PricingChartDiscountStatus).InclusiveBetween(1, 3);
        }
    }
}
