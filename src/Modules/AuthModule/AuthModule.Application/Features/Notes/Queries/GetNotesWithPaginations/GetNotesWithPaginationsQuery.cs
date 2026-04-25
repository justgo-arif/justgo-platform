using AuthModule.Application.DTOs.Notes;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Notes.Queries.GetNotesWithPaginations
{
    public class GetNotesWithPaginationsQuery : PaginationParams, IRequest<PagedResult<NoteDTO>>
    {
        public int EntityType { get; set; }
        public Guid EntityId { get; set; }
        public string Module { get; set; }
    }
}
