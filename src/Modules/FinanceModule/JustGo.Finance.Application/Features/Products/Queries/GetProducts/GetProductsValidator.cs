using FluentValidation;

namespace JustGo.Finance.Application.Features.Products.Queries.GetProducts
{
    public class GetProductsValidator : AbstractValidator<GetProductsQuery>
    {
        public GetProductsValidator()
        {
            RuleFor(r => r.MerchantId).NotEmpty().WithMessage("Merchant Id is required.");
            RuleFor(r => r.PageSize).NotEmpty().WithMessage("Page Size is required.");
        }
    }
}
