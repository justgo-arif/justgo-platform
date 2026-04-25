using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Notes.Commands.DeleteNotes
{
    public class DeleteNotesCommand : IRequest<int>
    {
        public DeleteNotesCommand(Guid notesGuid, string module)
        {
            NotesGuid = notesGuid;
            Module = module;
        }

        public Guid NotesGuid { get; set; }
        public string Module { get; set; }
    }
}
