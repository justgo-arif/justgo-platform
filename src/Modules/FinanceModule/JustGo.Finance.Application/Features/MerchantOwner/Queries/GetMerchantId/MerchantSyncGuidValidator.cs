using FluentValidation;

namespace JustGo.Finance.Application.Features.MerchantOwner.Queries.GetMerchantId
{
    public class MerchantSyncGuidValidator : AbstractValidator<GetMerchantIdQuery>
    {
        public MerchantSyncGuidValidator()
        {
            RuleFor(r => r.MerchantSyncGuid).NotEmpty().WithMessage("MerchantId is required.");
        }
    }
}
