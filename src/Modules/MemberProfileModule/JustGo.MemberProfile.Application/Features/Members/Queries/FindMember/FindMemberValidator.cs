using FluentValidation;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.FindMember;

public class FindMemberValidator : AbstractValidator<FindMemberQuery>
{
    public FindMemberValidator()
    {
        RuleFor(x => x.MID).NotNull().NotEmpty().WithMessage("MID is required.");
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Email) || !string.IsNullOrWhiteSpace(x.LastName))
            .WithMessage("Either Email or LastName must be provided with MID.");
    }
}
