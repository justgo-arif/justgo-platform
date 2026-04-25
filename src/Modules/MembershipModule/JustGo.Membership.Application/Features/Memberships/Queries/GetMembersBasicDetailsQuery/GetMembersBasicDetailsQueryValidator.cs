using FluentValidation;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetMembersBasicDetailsQuery
{
    public class GetMembersBasicDetailsQueryValidator : AbstractValidator<GetMembersBasicDetailsQuery>
    {
        public GetMembersBasicDetailsQueryValidator()
        {
            RuleFor(x => x.MemberDocIds)
                .NotNull().WithMessage("MemberDocIds cannot be null.")
                .NotEmpty().WithMessage("MemberDocIds must contain at least one document ID.")
                .ForEach(x => x.GreaterThan(0).WithMessage("Each MemberDocId must be greater than zero."));
        }
    }
}
