using System.Data;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Infrastructure.Files;

namespace JustGo.Authentication.Services.Interfaces.Files
{
    public interface IAttachmentService
    {
#if NET9_0_OR_GREATER
        Task<List<Attachment>> GetAttachments(Guid entityId, int entityType, string module, CancellationToken cancellationToken);
        Task<PagedResult<Attachment>> GetAttachments(Guid entityId, int entityType, string module, PaginationParams paginationParams, CancellationToken cancellationToken);
        Task<KeysetPagedResult<Attachment>> GetAttachments(Guid entityId, int entityType, string module, KeysetPaginationParams paginationParams, CancellationToken cancellationToken);
        Task<int> CreateAttachment(Attachment attachment, IDbTransaction transaction, CancellationToken cancellationToken);
        Task<int> EditAttachment(Attachment attachment, IDbTransaction transaction, CancellationToken cancellationToken);
        Task<int> DeleteAttachment(Guid id, string module, IDbTransaction transaction, CancellationToken cancellationToken);
        Task<Attachment> GetAttachment(Guid attachmentGuid, string module, CancellationToken cancellationToken);
#endif
    }
}
