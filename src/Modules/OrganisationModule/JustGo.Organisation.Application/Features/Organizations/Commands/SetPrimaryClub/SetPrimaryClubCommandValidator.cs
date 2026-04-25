using FluentValidation;

namespace JustGo.Organisation.Application.Features.Organizations.Commands.SetPrimaryClub
{
    public class SetPrimaryClubCommandValidator : AbstractValidator<SetPrimaryClubCommand>
    {
        public SetPrimaryClubCommandValidator()
        {
            RuleFor(x => x.MemberSyncGuid).NotEmpty();
            RuleFor(x => x.ClubMemberId).NotEmpty();
        }
    }
}
