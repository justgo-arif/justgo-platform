using AuthModule.Application.DTOs.Attachments;
using JustGo.Authentication.Helper.Paginations.Offset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Files;
using MapsterMapper;

namespace AuthModule.Application.Features.Files.Queries.GetAttachmentsWithPaginations;

public class GetAttachmentsWithPaginationsHandler : IRequestHandler<GetAttachmentsWithPaginationsQuery, PagedResult<AttachmentDto>>
{
    private readonly IAttachmentService _attachmentService;
    private readonly IMapper _mapper;

    public GetAttachmentsWithPaginationsHandler(IAttachmentService attachmentService, IMapper mapper)
    {
        _attachmentService = attachmentService;
        _mapper = mapper;
    }

    public async Task<PagedResult<AttachmentDto>> Handle(GetAttachmentsWithPaginationsQuery request, CancellationToken cancellationToken)
    {
        var paginationParams = new PaginationParams
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
        var results = await _attachmentService.GetAttachments(request.EntityId, request.EntityType, request.Module, paginationParams, cancellationToken);
        return _mapper.Map<PagedResult<AttachmentDto>>(results);
    }
}
