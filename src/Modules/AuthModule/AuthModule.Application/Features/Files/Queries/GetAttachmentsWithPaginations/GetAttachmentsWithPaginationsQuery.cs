using AuthModule.Application.DTOs.Attachments;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Files.Queries.GetAttachmentsWithPaginations;

public class GetAttachmentsWithPaginationsQuery : PaginationParams, IRequest<PagedResult<AttachmentDto>>
{
    public int EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string Module { get; set; }
}
