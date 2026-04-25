using FluentValidation;

namespace JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Commands.DeleteUserEmergencyContacts
{
    public class DeleteUserEmergencyContactValidator : AbstractValidator<DeleteUserEmergencyContactCommand>
    {
        public DeleteUserEmergencyContactValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Id is required.");
        }
    }
}
