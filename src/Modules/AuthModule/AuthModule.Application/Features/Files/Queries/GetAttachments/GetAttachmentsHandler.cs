using AuthModule.Application.DTOs.Attachments;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Files;
using MapsterMapper;

namespace AuthModule.Application.Features.Files.Queries.GetAttachments;

public class GetAttachmentsHandler : IRequestHandler<GetAttachmentsQuery, List<AttachmentDto>>
{
    private readonly IAttachmentService _attachmentService;
    private readonly IMapper _mapper;

    public GetAttachmentsHandler(IAttachmentService attachmentService, IMapper mapper)
    {
        _attachmentService = attachmentService;
        _mapper = mapper;
    }

    public async Task<List<AttachmentDto>> Handle(GetAttachmentsQuery request, CancellationToken cancellationToken)
    {
        var results = await _attachmentService.GetAttachments(request.EntityId, request.EntityType, request.Module, cancellationToken);
        return _mapper.Map<List<AttachmentDto>>(results);
    }
}
