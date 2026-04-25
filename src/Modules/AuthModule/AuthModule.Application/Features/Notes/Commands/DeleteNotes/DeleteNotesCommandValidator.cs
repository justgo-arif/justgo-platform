using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace AuthModule.Application.Features.Notes.Commands.DeleteNotes
{
    public class DeleteNotesCommandValidator : AbstractValidator<DeleteNotesCommand>
    {
        public DeleteNotesCommandValidator() 
        {
            RuleFor(r => r.NotesGuid).NotNull().NotEmpty().WithMessage("Id is required.");
            RuleFor(r => r.Module).NotNull().NotEmpty().WithMessage("Module is required.");
        }
    }
}
