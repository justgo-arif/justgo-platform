using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Application.DTOs.Notes;
using AuthModule.Application.Features.Notes.Queries.GetNotes;
using MapsterMapper;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.Notes;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Notes.Queries.GetNotesWithPaginations
{
    public class GetNotesWithPaginationsHandler : IRequestHandler<GetNotesWithPaginationsQuery, PagedResult<NoteDTO>>
    {
        private readonly INoteService _noteService;
        private readonly IMapper _mapper;

        public GetNotesWithPaginationsHandler(INoteService noteService, IMapper mapper)
        {
            _noteService = noteService;
            _mapper = mapper;
        }

        public async Task<PagedResult<NoteDTO>> Handle(GetNotesWithPaginationsQuery request, CancellationToken cancellationToken)
        {
            var paginationParams = new PaginationParams
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
            var results = await _noteService.GetNotes(request.EntityId, request.EntityType, request.Module, paginationParams, cancellationToken);
            var noteDto = _mapper.Map<PagedResult<NoteDTO>>(results);
            return noteDto;
        }
    }
}
