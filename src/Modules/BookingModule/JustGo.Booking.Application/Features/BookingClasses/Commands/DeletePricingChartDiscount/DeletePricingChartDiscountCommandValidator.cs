using FluentValidation;

namespace JustGo.Booking.Application.Features.BookingClasses.Commands.DeletePricingChartDiscount
{
    public class DeletePricingChartDiscountCommandValidator : AbstractValidator<DeletePricingChartDiscountCommand>
    {
        public DeletePricingChartDiscountCommandValidator()
        {
            RuleFor(r => r.PricingChartDiscountId)
                .NotEmpty().WithMessage("Id is required.")
                .GreaterThan(0).WithMessage("Id must be greater than zero.");
        }
    }
}
