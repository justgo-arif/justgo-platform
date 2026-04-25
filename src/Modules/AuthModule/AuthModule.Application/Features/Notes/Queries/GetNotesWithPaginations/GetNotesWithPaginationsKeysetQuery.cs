using AuthModule.Application.DTOs.Notes;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Notes.Queries.GetNotesWithPaginations
{
    public class GetNotesWithPaginationsKeysetQuery : KeysetPaginationParams, IRequest<KeysetPagedResult<NoteDTO>>
    {
        public int EntityType { get; set; }
        public Guid EntityId { get; set; }
        public string Module { get; set; }
    }
}
