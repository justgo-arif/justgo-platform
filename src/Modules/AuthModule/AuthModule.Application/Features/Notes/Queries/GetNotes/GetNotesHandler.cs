using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Application.DTOs.Notes;
using MapsterMapper;
using JustGo.Authentication.Infrastructure.Notes;
using JustGo.Authentication.Services.Interfaces.Notes;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Notes.Queries.GetNotes
{
    public class GetNotesHandler : IRequestHandler<GetNotesQuery, List<NoteDTO>>
    {
        private readonly INoteService _noteService;
        private readonly IMapper _mapper;

        public GetNotesHandler(INoteService noteService, IMapper mapper)
        {
            _noteService = noteService;
            _mapper = mapper;
        }

        public async Task<List<NoteDTO>> Handle(GetNotesQuery request, CancellationToken cancellationToken)
        {
            var results = await _noteService.GetNotes(request.EntityId, request.EntityType, request.Module, cancellationToken);
            var noteDto = _mapper.Map<List<NoteDTO>>(results);
            return noteDto;
        }
    }
}
