using FluentValidation;

namespace JustGo.MemberProfile.Application.Features.MemberNotes.Commands.SaveMemberNotes
{
    public class SaveMemberNotesValidator : AbstractValidator<SaveMemberNotesCommand>
    {
        public SaveMemberNotesValidator()
        {
            RuleFor(x => x.EntityId)
                .NotEmpty()
                .WithMessage("EntityId is required");

            RuleFor(x => x.CategoryId)
                .GreaterThan(0)
                .WithMessage("CategoryId must be greater than 0");

            RuleFor(x => x.OwnerGuid)
                .NotEmpty()
                .WithMessage("Owner GUID is required")
                .Must(BeValidGuid)
                .WithMessage("Owner GUID must be a valid GUID format");

            RuleFor(x => x.MemberNoteId)
                .GreaterThanOrEqualTo(0)
                .WithMessage("MemberNoteId must be greater than or equal to 0");

            RuleFor(x => x.Details)
                .NotEmpty()
                .WithMessage("Note details are required")
                .MaximumLength(4000)
                .WithMessage("Note details must not exceed 4000 characters");
        }

        private static bool BeValidGuid(string guid)
        {
            return Guid.TryParse(guid, out _);
        }
    }
}