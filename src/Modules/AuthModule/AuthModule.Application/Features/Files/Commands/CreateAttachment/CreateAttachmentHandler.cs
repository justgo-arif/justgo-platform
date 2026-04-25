using JustGo.Authentication.Infrastructure.Files;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Files;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.Files.Commands.CreateAttachment;

public class CreateAttachmentHandler : IRequestHandler<CreateAttachmentCommand, int>
{
    private readonly IAttachmentService _attachmentService;
    private readonly IAzureBlobFileService _azureBlobFileService;
    private readonly IUnitOfWork _unitOfWork;
    public CreateAttachmentHandler(IAttachmentService attachmentService, IAzureBlobFileService azureBlobFileService, IUnitOfWork unitOfWork)
    {
        _attachmentService = attachmentService;
        _azureBlobFileService = azureBlobFileService;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CreateAttachmentCommand request, CancellationToken cancellationToken)
    {
        var fileType = Path.GetExtension(request.File.FileName).ToLower();
        var blobFileName = $"{Guid.NewGuid()}{fileType}";
        var destBlobPath = await _azureBlobFileService.MapPath($"~/store/{request.Module}_attachments/{request.EntityId}/{blobFileName}");

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Upload file to Azure Blob Storage
            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await request.File.CopyToAsync(memoryStream, cancellationToken);
                fileBytes = memoryStream.ToArray();
            }

            await _azureBlobFileService.UploadFileAsync(destBlobPath, fileBytes, FileMode.Create, cancellationToken);

            // Save attachment data to database
            var attachment = new Attachment
            {
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                Module = request.Module,
                Name = request.File.FileName,
                GeneratedName = blobFileName,
                Size = request.File.Length
            };

            var result = await _attachmentService.CreateAttachment(attachment, transaction, cancellationToken);

            await _unitOfWork.CommitAsync(transaction);

            return result;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(transaction);
            try
            {
                await _azureBlobFileService.DeleteFileAsync(destBlobPath, cancellationToken);
            }
            catch (Exception deleteEx)
            {
                throw new Exception($"Failed to delete uploaded file during rollback: {deleteEx.Message}");
            }
            throw new InvalidOperationException($"Failed to create attachment: {ex.Message}", ex);
        }
    }

}