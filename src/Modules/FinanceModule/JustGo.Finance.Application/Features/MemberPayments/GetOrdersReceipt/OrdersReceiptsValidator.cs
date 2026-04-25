using FluentValidation;

namespace JustGo.Finance.Application.Features.MemberPayments.GetOrdersReceipt
{
    public class OrdersReceiptsValidator : AbstractValidator<GetOrdersReceiptQuery>
    {
        public OrdersReceiptsValidator()
        {
            RuleFor(r => r.MerchantId).NotEmpty().WithMessage("Merchant Id is required.");
            RuleFor(r => r.PageNo).NotEmpty().WithMessage("Page No is required.");
            RuleFor(r => r.PageSize).NotEmpty().WithMessage("Page Size is required.");
        }
    }
}
