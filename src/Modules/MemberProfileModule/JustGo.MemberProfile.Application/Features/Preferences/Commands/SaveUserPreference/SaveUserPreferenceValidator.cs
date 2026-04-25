using FluentValidation;

namespace JustGo.MemberProfile.Application.Features.Preferences.Commands.SaveUserPreference
{
    public class SaveUserPreferenceValidator : AbstractValidator<SaveUserPreferenceCommand>
    {
        public SaveUserPreferenceValidator()
        {


            When(x => x is not null, () =>
            {
                RuleFor(x => x.MemberDocId)
                    .NotEmpty();


                RuleFor(x => x.PreferenceTypeId)
                    .GreaterThan(0);

                RuleFor(x => x.PreferenceValue)
                    .MaximumLength(500)
                    .When(x => x.PreferenceValue is not null);
            });
        }
    }
}