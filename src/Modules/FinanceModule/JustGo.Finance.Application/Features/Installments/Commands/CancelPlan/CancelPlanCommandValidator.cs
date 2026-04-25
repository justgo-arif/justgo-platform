using FluentValidation;

namespace JustGo.Finance.Application.Features.Installments.Commands.CancelPlan
{
    public class CancelPlanCommandValidator : AbstractValidator<CancelPlanCommand>
    {
        public CancelPlanCommandValidator()
        {
            RuleFor(x => x.PlanId)
                .NotNull().WithMessage("PlanIds is required.");

        }
    }
}
