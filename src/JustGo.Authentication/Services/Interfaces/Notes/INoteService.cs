using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Infrastructure.Notes;

namespace JustGo.Authentication.Services.Interfaces.Notes
{
    public interface INoteService
    {
#if NET9_0_OR_GREATER
        Task<List<Note>> GetNotes(Guid entityId, int entityType, string module, CancellationToken cancellationToken);
        Task<PagedResult<Note>> GetNotes(Guid entityId, int entityType, string module, PaginationParams paginationParams, CancellationToken cancellationToken);
        Task<KeysetPagedResult<Note>> GetNotes(Guid entityId, int entityType, string module, KeysetPaginationParams paginationParams, CancellationToken cancellationToken);
        Task<int> CreateNote(Note note, CancellationToken cancellationToken);
        Task<int> EditNote(Note note, CancellationToken cancellationToken);
        Task<int> DeleteNote(Guid id, string module, CancellationToken cancellationToken);
#endif
    }
}
