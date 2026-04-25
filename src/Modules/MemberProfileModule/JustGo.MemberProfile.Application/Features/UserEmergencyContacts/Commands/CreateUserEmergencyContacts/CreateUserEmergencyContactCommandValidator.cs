using FluentValidation;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Queries.GetEmergencyContactMandatorySettings;

namespace JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Commands.CreateUserEmergencyContacts
{
    public class CreateUserEmergencyContactCommandValidator : AbstractValidator<CreateUserEmergencyContactCommand>
    {
        private readonly IMediator _mediator;

        public CreateUserEmergencyContactCommandValidator(IMediator mediator)
        {
            _mediator = mediator;

            RuleFor(x => x.UserSyncGuid)
                .NotEmpty().WithMessage("UserSyncGuid is required.");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("FirstName is required.")
                .MaximumLength(50);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("LastName is required.")
                .MaximumLength(50);

            RuleFor(x => x.Relation)
                .NotEmpty().WithMessage("Relation is required.")
                .MaximumLength(100);
            RuleFor(x => x.ContactNumber)
                .MaximumLength(50)
                .When(x => !string.IsNullOrWhiteSpace(x.ContactNumber))
                .WithMessage("Contact Number must be 50 characters or less");
            RuleFor(x => x.EmailAddress)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.EmailAddress))
                .WithMessage("Email Address must be 100 characters or less");
            RuleFor(x => x.CountryCode)
                .MaximumLength(20)
                .When(x => !string.IsNullOrWhiteSpace(x.CountryCode))
                .WithMessage("Country Code must be 20 characters or less");

            //RuleFor(x => x.CountryCode)
            //    .NotEmpty().WithMessage("CountryCode is required.")
            //    .MaximumLength(10);

            // IsPrimary is optional: no rule required
        }

        public override async Task<FluentValidation.Results.ValidationResult> ValidateAsync(
            FluentValidation.ValidationContext<CreateUserEmergencyContactCommand> context,
            CancellationToken cancellation = default)
        {
            var result = await base.ValidateAsync(context, cancellation);

            // Fetch latest settings and apply dynamic rules
            var settings = await _mediator.Send(new GetEmergencyContactMandatorySettingsQuery(), cancellation);
            var cmd = context.InstanceToValidate;

            // Email dynamic rule
            if (settings.EmailMandatory)
            {
                if (string.IsNullOrWhiteSpace(cmd.EmailAddress))
                {
                    // Latest settings say mandatory. If the command indicated not mandatory, prefer latest and show specific message.
                    if (!cmd.EmailMandatory)
                        result.Errors.Add(new FluentValidation.Results.ValidationFailure(nameof(cmd.EmailAddress), "Field EmailAddress is mandatory as per latest configuration"));
                    else
                        result.Errors.Add(new FluentValidation.Results.ValidationFailure(nameof(cmd.EmailAddress), "EmailAddress is required"));
                }
                else
                {
                    // Optional: format check
                    if (!IsValidEmail(cmd.EmailAddress!))
                        result.Errors.Add(new FluentValidation.Results.ValidationFailure(nameof(cmd.EmailAddress), "EmailAddress is invalid"));
                }
            }

            // Phone dynamic rule
            if (settings.PhoneMandatory)
            {
                if (string.IsNullOrWhiteSpace(cmd.ContactNumber))
                {
                    if (!cmd.PhoneMandatory)
                        result.Errors.Add(new FluentValidation.Results.ValidationFailure(nameof(cmd.ContactNumber), "Field Contact Number is mandatory as per latest configuration"));
                    else
                        result.Errors.Add(new FluentValidation.Results.ValidationFailure(nameof(cmd.ContactNumber), "Contact Number is required"));
                }
            }

            return result;
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}