using FluentValidation;

namespace JustGo.Booking.Application.Features.BookingClasses.Commands.AddPricingChartDiscount
{
    public class AddPricingChartDiscountCommandValidator : AbstractValidator<AddPricingChartDiscountCommand>
    {
        public AddPricingChartDiscountCommandValidator()
        {
            RuleFor(x => x.PricingChartDiscountName).NotEmpty();
            RuleFor(x => x.PricingChartDiscountType).InclusiveBetween(0, 1);
            RuleFor(x => x.PricingChartDiscountValue).GreaterThan(0);
            RuleFor(x => x.PricingChartDiscountStatus).InclusiveBetween(1, 3);
            RuleFor(x => x.PricingChartIds).NotNull().Must(x => x.Count > 0);
        }
    }
}
