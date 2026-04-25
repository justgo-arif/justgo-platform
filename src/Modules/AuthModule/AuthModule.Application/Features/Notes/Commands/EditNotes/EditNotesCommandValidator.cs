using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace AuthModule.Application.Features.Notes.Commands.EditNotes
{
    public class EditNotesCommandValidator : AbstractValidator<EditNotesCommand>
    {
        public EditNotesCommandValidator()
        {
            RuleFor(r => r.NotesGuid).NotNull().NotEmpty().WithMessage("Id is required.");
            RuleFor(r => r.EntityType).NotEmpty().WithMessage("Entity Type is required.")
               .GreaterThan(0).WithMessage("Entity Type must be greater than zero.");
            RuleFor(r => r.EntityId).NotNull().NotEmpty().WithMessage("Entity Id is required.");
            RuleFor(r => r.Details).NotNull().NotEmpty().WithMessage("Notes detail is required.");
            RuleFor(r => r.Module).NotNull().NotEmpty().WithMessage("Module is required.");
        }
    }
}
