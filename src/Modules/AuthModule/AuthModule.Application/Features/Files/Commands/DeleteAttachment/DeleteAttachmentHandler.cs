using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Files;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.Files.Commands.DeleteAttachment;

public class DeleteAttachmentHandler : IRequestHandler<DeleteAttachmentCommand, int>
{
    private readonly IAttachmentService _attachmentService;
    private readonly IAzureBlobFileService _azureBlobFileService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAttachmentHandler(IAttachmentService attachmentService, IAzureBlobFileService azureBlobFileService, IUnitOfWork unitOfWork)
    {
        _attachmentService = attachmentService;
        _azureBlobFileService = azureBlobFileService;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(DeleteAttachmentCommand request, CancellationToken cancellationToken)
    {
        var attachment = await _attachmentService.GetAttachment(request.AttachmentId, request.Module, cancellationToken);
        var destBlobPath = await _azureBlobFileService.MapPath($"~/store/{request.Module}_attachments/{request.EntityId}/{attachment.GeneratedName}");
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _azureBlobFileService.DeleteFileAsync(destBlobPath, cancellationToken);
            var result = await _attachmentService.DeleteAttachment(request.AttachmentId, request.Module, transaction, cancellationToken);
            await _unitOfWork.CommitAsync(transaction);
            return result;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(transaction);
            throw new InvalidOperationException($"Failed to delete attachment: {ex.Message}", ex);
        }
    }
}