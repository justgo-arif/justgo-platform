using FluentValidation;

namespace JustGo.MemberProfile.Application.Features.Members.Commands.AddFamilyMember
{
    public class AddFamilyMemberCommandValidator : AbstractValidator<AddFamilyMemberCommand>
    {
        public AddFamilyMemberCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.UserId).GreaterThan(0);
            RuleFor(x => x.MemberDocIds).NotNull();
        }
    }
}
