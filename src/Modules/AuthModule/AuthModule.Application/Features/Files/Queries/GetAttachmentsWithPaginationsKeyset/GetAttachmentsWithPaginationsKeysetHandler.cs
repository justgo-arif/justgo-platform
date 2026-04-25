using AuthModule.Application.DTOs.Attachments;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Files;
using MapsterMapper;

namespace AuthModule.Application.Features.Files.Queries.GetAttachmentsWithPaginationsKeyset;

public class GetAttachmentsWithPaginationsKeysetHandler : IRequestHandler<GetAttachmentsWithPaginationsKeysetQuery, KeysetPagedResult<AttachmentDto>>
{
    private readonly IAttachmentService _attachmentService;
    private readonly IMapper _mapper;

    public GetAttachmentsWithPaginationsKeysetHandler(IAttachmentService attachmentService, IMapper mapper)
    {
        _attachmentService = attachmentService;
        _mapper = mapper;
    }

    public async Task<KeysetPagedResult<AttachmentDto>> Handle(GetAttachmentsWithPaginationsKeysetQuery request, CancellationToken cancellationToken)
    {
        var paginationParams = new KeysetPaginationParams
        {
            LastSeenId = request.LastSeenId,
            PageSize = request.PageSize
        };
        var results = await _attachmentService.GetAttachments(request.EntityId, request.EntityType, request.Module, paginationParams, cancellationToken);
        var attachmentDto = _mapper.Map<KeysetPagedResult<AttachmentDto>>(results);
        return attachmentDto;
    }
}
