using FluentValidation;

namespace AuthModule.Application.Features.Notes.Commands.CreateNotes
{
    public class CreateNotesCommandValidator : AbstractValidator<CreateNotesCommand>
    {
        public CreateNotesCommandValidator()
        {
            RuleFor(r => r.EntityType).NotEmpty().WithMessage("Entity Type is required.")
               .GreaterThan(0).WithMessage("Entity Type must be greater than zero.");
            RuleFor(r => r.EntityId).NotNull().NotEmpty().WithMessage("Entity Id is required.");
            RuleFor(r => r.Details).NotNull().NotEmpty().WithMessage("Notes detail is required.");
            RuleFor(r => r.Module).NotNull().NotEmpty().WithMessage("Module is required.");
        }
    }
}
