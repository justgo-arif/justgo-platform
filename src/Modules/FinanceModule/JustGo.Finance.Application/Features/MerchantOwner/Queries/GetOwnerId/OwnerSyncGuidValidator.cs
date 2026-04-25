using FluentValidation;

namespace JustGo.Finance.Application.Features.MerchantOwner.Queries.GetOwnerId
{

    public class OwnerSyncGuidValidator : AbstractValidator<GetOwnerIdQuery>
    {
        public OwnerSyncGuidValidator()
        {
            RuleFor(r => r.OwnerId).NotEmpty().WithMessage("OwnerId is required.");
        }
    }
}
