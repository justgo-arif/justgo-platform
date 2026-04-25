using FluentValidation;

namespace JustGo.Finance.Application.Features.PaymentRefund.Commands.CreateRefundPayment
{
    public class CreateRefundPaymentValidator : AbstractValidator<CreateRefundPaymentCommand>
    {
        public CreateRefundPaymentValidator(RefundItemValidator refundItemValidator)
        {
            RuleFor(x => x.PaymentId)
                .NotEmpty()
                .WithMessage("PaymentId is required.");

            RuleFor(x => x.RefundReasonId)
                .GreaterThan(0)
                .WithMessage("Refund reason must be selected.");

            RuleFor(x => x.RefundItems)
                .NotNull()
                .WithMessage("Refund items are required.");

            RuleForEach(x => x.RefundItems)
                .SetValidator(refundItemValidator);
        }
    }

}
