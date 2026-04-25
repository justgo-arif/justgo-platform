using FluentValidation;

namespace JustGo.MemberProfile.Application.Features.Members.Commands.UpdateMember;

public class UpdateMemberCommandValidator : AbstractValidator<UpdateMemberCommand>
{
    public UpdateMemberCommandValidator()
    {
        RuleFor(x => x.UserSyncId)
            .NotEmpty()
            .WithMessage("SyncGuid is required.");

        RuleFor(x => x.LoginId)
            .NotEmpty()
            .WithMessage("Username is required.")
            .MaximumLength(100)
            .WithMessage("Username cannot exceed 100 characters.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First Name is required.")
            .MaximumLength(100)
            .WithMessage("First Name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last Name is required.")
            .MaximumLength(100)
            .WithMessage("Last Name cannot exceed 100 characters.");

        RuleFor(x => x.EmailAddress)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email must be a valid email address.")
            .MaximumLength(256)
            .WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.DOB)
            .NotEmpty()
            .WithMessage("Date of Birth is required.")
            .LessThan(DateTime.Today)
            .WithMessage("Date of Birth must be in the past.");

        RuleFor(x => x.Gender)
            .NotEmpty()
            .WithMessage("Gender is required.");

        RuleFor(x => x.Country)
            .NotEmpty()
            .WithMessage("Country is required.")
            .MaximumLength(100)
            .WithMessage("Country cannot exceed 100 characters.");

        //RuleFor(x => x.CountryId)
        //    .GreaterThan(0)
        //    .WithMessage("CountryId must be greater than 0.");

        When(x => !string.IsNullOrWhiteSpace(x.Mobile), () =>
        {
            RuleFor(x => x.Mobile)
                .Matches(@"^[\d\s\-\+\(\)]+$")
                .WithMessage("Mobile number contains invalid characters.")
                .MaximumLength(20)
                .WithMessage("Mobile number cannot exceed 20 characters.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Address1), () =>
        {
            RuleFor(x => x.Address1)
                .MaximumLength(200)
                .WithMessage("Address1 cannot exceed 200 characters.");
        });
    }
}