using FluentValidation;

namespace JustGo.MemberProfile.Application.Features.Preferences.Commands.SaveCurrentPreference;

public class SaveCurrentPreferenceValidator : AbstractValidator<SaveCurrentPreferenceCommand>
{
    public SaveCurrentPreferenceValidator()
    {
        RuleFor(x => x.OptinId)
            .GreaterThan(0)
            .WithMessage("OptinId must be greater than 0.");
    }
}