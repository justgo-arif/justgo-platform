using FluentValidation;

namespace JustGo.Finance.Application.Features.Installments.Commands.UpdatePaymentSchedule
{
    public class UpdatePaymentScheduleCommandValidator : AbstractValidator<UpdatePaymentScheduleCommand>
    {
        public UpdatePaymentScheduleCommandValidator()
        {
            RuleFor(x => x.PlanId)
                .NotNull().WithMessage("PlanId is required.");
            RuleFor(x => x.UpdateRequest.Id)
                .NotNull().WithMessage("Id is required.");
            RuleFor(x => x.UpdateRequest.PaymentDate)
                .NotEmpty().WithMessage("Payment date is required.");

        }
    }
}
