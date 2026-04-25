using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Application.DTOs.Notes;
using MapsterMapper;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Services.Interfaces.Notes;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Notes.Queries.GetNotesWithPaginations
{
    public class GetNotesWithPaginationsKeysetHandler : IRequestHandler<GetNotesWithPaginationsKeysetQuery, KeysetPagedResult<NoteDTO>>
    {
        private readonly INoteService _noteService;
        private readonly IMapper _mapper;

        public GetNotesWithPaginationsKeysetHandler(INoteService noteService, IMapper mapper)
        {
            _noteService = noteService;
            _mapper = mapper;
        }

        public async Task<KeysetPagedResult<NoteDTO>> Handle(GetNotesWithPaginationsKeysetQuery request, CancellationToken cancellationToken)
        {
            var paginationParams = new KeysetPaginationParams
            {
                LastSeenId = request.LastSeenId,
                PageSize = request.PageSize
            };
            var results = await _noteService.GetNotes(request.EntityId, request.EntityType, request.Module, paginationParams, cancellationToken);
            var noteDto = _mapper.Map<KeysetPagedResult<NoteDTO>>(results);
            return noteDto;
        }
    }
}
