using FluentValidation;

namespace JustGo.Organisation.Application.Features.Organizations.Commands.AddClub;

public class JoinClubCommandValidator : AbstractValidator<JoinClubCommand>
{
    public JoinClubCommandValidator()
    {
        RuleFor(x => x.ClubGuid).NotEmpty()
            .WithMessage("ClubGuid is required.");

        RuleFor(x => x.MemberGuid).NotEmpty()
            .WithMessage("MemberGuid is required.");

        RuleFor(x => x.ClubMemberRoles).NotEmpty()
            .WithMessage("ClubMemberRoles is required.");
    }
}
