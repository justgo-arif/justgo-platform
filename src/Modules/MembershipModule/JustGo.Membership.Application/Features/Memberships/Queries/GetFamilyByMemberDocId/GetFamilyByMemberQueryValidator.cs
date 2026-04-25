using FluentValidation;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetFamilyByMemberDocId
{
    public class GetFamilyByMemberQueryValidator : AbstractValidator<GetFamilyByMemberQuery>
    {
        public GetFamilyByMemberQueryValidator()
        {
            RuleFor(x => x.MemberDocId)
                .NotNull().WithMessage("MemberDocIds cannot be null.")
                .NotEmpty().WithMessage("MemberDocIds must contain at least one document ID.")
                .GreaterThan(0).WithMessage("MemberDocId must be greater than zero.");
        }
    }
}
