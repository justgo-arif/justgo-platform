using FluentValidation;

namespace JustGo.Finance.Application.Features.PaymentAccount.Commands.UpdateStatementDescriptor;

public class UpdateStatementDescriptorValidator : AbstractValidator<UpdateStatementDescriptorCommand>
{
    public UpdateStatementDescriptorValidator()
    {
        RuleFor(x => x.MerchantId)
           .NotEmpty().WithMessage("MerchantId is required.");
    }
}
