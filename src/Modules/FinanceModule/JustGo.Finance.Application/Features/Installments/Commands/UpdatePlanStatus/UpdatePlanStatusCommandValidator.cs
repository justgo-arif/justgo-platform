using FluentValidation;
using JustGo.Finance.Application.Features.Installments.Commands.CancelledInstallment;

namespace JustGo.Finance.Application.Features.Installments.Commands.UpdatePlanStatus
{
    public class UpdatePlanStatusCommandValidator : AbstractValidator<UpdatePlanStatusCommand>
    {
        public UpdatePlanStatusCommandValidator()
        {
            RuleFor(x => x.PlanId)
                .NotNull().WithMessage("PlanIds is required.");

        }
    }
}
