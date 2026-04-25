using FluentValidation;


namespace JustGo.Booking.Application.Features.ClassManagement.Queries.ProRataCalculation
{
    public class ProRataCalculationValidator : AbstractValidator<ProRataCalculationQuery>
    {
        public ProRataCalculationValidator()
        {
            RuleFor(x => x.Request)
                .NotNull().WithMessage("Request is required.")
                .DependentRules(() =>
                {
                    RuleFor(x => x.Request.ClassProductId)
                        .GreaterThan(0).WithMessage("ClassProductId is required.");

                    RuleFor(x => x.Request.StartDate)
                        .NotEmpty().WithMessage("StartDate is required.");
                });
        }
    }
}
