using AuthModule.Application.DTOs.Attachments;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Files.Queries.GetAttachmentsWithPaginationsKeyset;

public class GetAttachmentsWithPaginationsKeysetQuery : KeysetPaginationParams, IRequest<KeysetPagedResult<AttachmentDto>>
{
    public int EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string Module { get; set; }
}
