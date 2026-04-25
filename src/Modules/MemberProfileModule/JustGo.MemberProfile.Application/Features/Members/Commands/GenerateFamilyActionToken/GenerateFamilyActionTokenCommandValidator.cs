using FluentValidation;

namespace JustGo.MemberProfile.Application.Features.Members.Commands.GenerateFamilyActionToken
{
    public class GenerateFamilyActionTokenCommandValidator : AbstractValidator<GenerateFamilyActionTokenCommand>
    {
        public GenerateFamilyActionTokenCommandValidator()
        {
            RuleFor(x => x.FamilyDocId)
                .GreaterThan(0).WithMessage("FamilyDocId must be a positive number");

            RuleFor(x => x.InitiateMemberDocId)
                .GreaterThan(0).WithMessage("InitiateMemberDocId must be a positive number");

            RuleFor(x => x.TargetMemberDocId)
                .GreaterThan(0).WithMessage("TargetMemberDocId must be a positive number");

            RuleFor(x => x.Url)
                .NotEmpty().WithMessage("Url is required")
                .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute))
                .WithMessage("Url must be a valid absolute URL");
        }
    }
}
