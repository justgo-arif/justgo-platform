using FluentValidation;

namespace JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Commands.SetPrimaryUserEmergencyContact
{
    public class SetPrimaryUserEmergencyContactCommandValidator : AbstractValidator<SetPrimaryUserEmergencyContactCommand>
    {
        public SetPrimaryUserEmergencyContactCommandValidator()
        {
            RuleFor(x => x.ContactId).GreaterThan(0);
        }
    }
}
