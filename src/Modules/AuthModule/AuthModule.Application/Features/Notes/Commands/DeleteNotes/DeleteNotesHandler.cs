using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.Notes;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Notes.Commands.DeleteNotes
{
    public class DeleteNotesHandler : IRequestHandler<DeleteNotesCommand, int>
    {
        private readonly INoteService _noteService;

        public DeleteNotesHandler(INoteService noteService)
        {
            _noteService = noteService;
        }

        public async Task<int> Handle(DeleteNotesCommand request, CancellationToken cancellationToken)
        {
            return await _noteService.DeleteNote(request.NotesGuid, request.Module, cancellationToken);
        }
    }
}
