using FluentValidation;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Queries.GetEmergencyContactMandatorySettings;

namespace JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Commands.UpdateUserEmergencyContacts
{
    public class UpdateUserEmergencyContactCommandValidator : AbstractValidator<UpdateUserEmergencyContactCommand>
    {
        private readonly IMediator _mediator;

        public UpdateUserEmergencyContactCommandValidator(IMediator mediator)
        {
            _mediator = mediator;

            // Static mandatory fields
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
            RuleFor(x => x.SyncGuid)
                .NotEmpty().WithMessage("SyncGuid is required.")
                .MaximumLength(36);

            // IsPrimary is optional: no rule
        }

        public override async Task<FluentValidation.Results.ValidationResult> ValidateAsync(ValidationContext<UpdateUserEmergencyContactCommand> context,CancellationToken cancellation = default)
        {
            var result = await base.ValidateAsync(context, cancellation);

            // Fetch latest configuration (has priority over command flags)
            var settings = await _mediator.Send(new GetEmergencyContactMandatorySettingsQuery(), cancellation);
            var cmd = context.InstanceToValidate;

            // Email dynamic rule
            if (settings.EmailMandatory)
            {
                if (string.IsNullOrWhiteSpace(cmd.EmailAddress))
                {
                    // If command thought it's not mandatory, show "latest configuration" message
                    if (!cmd.EmailMandatory)
                        result.Errors.Add(new FluentValidation.Results.ValidationFailure(
                            nameof(cmd.EmailAddress),
                            "Field EmailAddress is mandatory as per latest configuration"));
                    else
                        result.Errors.Add(new FluentValidation.Results.ValidationFailure(
                            nameof(cmd.EmailAddress),
                            "EmailAddress is required"));
                }
                else if (!IsValidEmail(cmd.EmailAddress))
                {
                    result.Errors.Add(new FluentValidation.Results.ValidationFailure(
                        nameof(cmd.EmailAddress),
                        "EmailAddress is invalid"));
                }
            }
            else
            {
                // Not mandatory by latest settings; if provided, optionally validate format
                if (!string.IsNullOrWhiteSpace(cmd.EmailAddress) && !IsValidEmail(cmd.EmailAddress))
                {
                    result.Errors.Add(new FluentValidation.Results.ValidationFailure(
                        nameof(cmd.EmailAddress),
                        "EmailAddress is invalid"));
                }
            }

            // Phone dynamic rule
            if (settings.PhoneMandatory)
            {
                if (string.IsNullOrWhiteSpace(cmd.ContactNumber))
                {
                    if (!cmd.PhoneMandatory)
                        result.Errors.Add(new FluentValidation.Results.ValidationFailure(
                            nameof(cmd.ContactNumber),
                            "Field Contact Number is mandatory as per latest configuration"));
                    else
                        result.Errors.Add(new FluentValidation.Results.ValidationFailure(
                            nameof(cmd.ContactNumber),
                            "Contact Number is required"));
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