using Adyen.Model.BalancePlatform;
using FluentValidation;

namespace JustGo.Finance.Application.Features.PaymentAccount.Commands.UpdateSweep;

public class UpdateSweepValidator : AbstractValidator<UpdateSweepCommand>
{
    public UpdateSweepValidator()
    {
        RuleFor(x => x.SweepId)
           .NotEmpty().WithMessage("SweepId is required.");

        RuleFor(x => x.SweepType)
            .IsInEnum().WithMessage("SweepType is not valid.")
            .Must(type => Enum.IsDefined(typeof(SweepSchedule.TypeEnum), type))
            .WithMessage("SweepType is required and must be a defined value.");
    }
}
