using FluentValidation;

namespace JustGo.Organisation.Application.Features.Organizations.Commands.ClubTransferRequest;

public class ClubTransferRequestCommandValidator : AbstractValidator<ClubTransferRequestCommand>
{
    public ClubTransferRequestCommandValidator()
    {
        RuleFor(x => x.MemberSyncGuid).NotEmpty().WithMessage("Member guid is required.");
        RuleFor(x => x.FromClubSyncGuid).NotEmpty().WithMessage("From club guid is required.");
        RuleFor(x => x.ToClubSyncGuid).NotEmpty().WithMessage("To club guid is required.");
        RuleFor(x => x.ReasonForMove).NotEmpty().WithMessage("Reason is required.");
    }
}
