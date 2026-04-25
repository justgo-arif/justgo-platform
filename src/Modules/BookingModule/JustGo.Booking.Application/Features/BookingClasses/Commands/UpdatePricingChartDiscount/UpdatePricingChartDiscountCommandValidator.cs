using FluentValidation;

namespace JustGo.Booking.Application.Features.BookingClasses.Commands.UpdatePricingChartDiscount
{
    public class UpdatePricingChartDiscountCommandValidator : AbstractValidator<UpdatePricingChartDiscountCommand>
    {
        public UpdatePricingChartDiscountCommandValidator()
        {
            RuleFor(x => x.PricingChartDiscountId).GreaterThan(0);
            RuleFor(x => x.PricingChartDiscountName).NotEmpty();
            RuleFor(x => x.PricingChartDiscountType).InclusiveBetween(0, 1);
            RuleFor(x => x.PricingChartDiscountValue).GreaterThan(0);
            RuleFor(x => x.PricingChartIds).NotNull().Must(x => x.Count > 0);
        }
    }
}
