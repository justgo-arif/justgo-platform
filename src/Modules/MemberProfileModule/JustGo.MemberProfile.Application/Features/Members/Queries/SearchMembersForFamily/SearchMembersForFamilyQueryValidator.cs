using FluentValidation;

namespace JustGo.MemberProfile.Application.Features.Members.Queries.SearchMembersForFamily
{
    public class SearchMembersForFamilyQueryValidator : AbstractValidator<SearchMembersForFamilyQuery>
    {
        public SearchMembersForFamilyQueryValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email must be provided.");

            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.MID) || x.DateOfBirth.HasValue)
                .WithMessage("Either MID or Date of Birth must be provided.");
        }
    }
}
