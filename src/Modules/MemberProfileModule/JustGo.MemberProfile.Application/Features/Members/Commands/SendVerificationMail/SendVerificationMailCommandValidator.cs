using FluentValidation;

namespace JustGo.MemberProfile.Application.Features.Members.Commands.SendVerificationMail;

public class SendVerificationMailCommandValidator : AbstractValidator<SendVerificationMailCommand>
{
    public SendVerificationMailCommandValidator()
    {
        RuleFor(x => x.UserSyncId)
            .NotEmpty()
            .WithMessage("UserSyncId is required.")
            .NotEqual(Guid.Empty)
            .WithMessage("UserSyncId must be a valid GUID.");

        RuleFor(r => r.Type)
            .NotEmpty()
            .WithMessage("Type is required.");
    }
}
