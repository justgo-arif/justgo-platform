using System.Data.Common;
using System.Text.Json;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.HostedServices;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SignalR;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.Common.Enums;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands.ResultProcessor;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands.Validators;
using JustGo.Result.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using SqlException = Microsoft.Data.SqlClient.SqlException;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands.ImportEquestrianResult;

public class EquestrianResultProcessor : IResultProcessor
{
    private readonly IReadRepositoryFactory _readRepoFactory;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IUtilityService _utilityService;
    private readonly IAzureBlobFileService _azureBlobFileService;
    private readonly IWriteRepositoryFactory _writeRepoFactory;

    public EquestrianResultProcessor(IReadRepositoryFactory readRepoFactory,
        IBackgroundTaskQueue backgroundTaskQueue, IUtilityService utilityService,
        IAzureBlobFileService azureBlobFileService, IWriteRepositoryFactory writeRepoFactory)
    {
        _readRepoFactory = readRepoFactory;
        _backgroundTaskQueue = backgroundTaskQueue;
        _utilityService = utilityService;
        _azureBlobFileService = azureBlobFileService;
        _writeRepoFactory = writeRepoFactory;
    }

    public async Task<Result<string>> ProcessAsync(ImportResultFileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var operationId = Guid.NewGuid().ToString();

            var result = await ImportResultHelper.ValidateRequiredFieldsMappedAsync(request, _readRepoFactory,
                cancellationToken);

            await ImportResultHelper.UpdateOperationIdInUploadedFile(_writeRepoFactory, request.FileDto.FileId,
                operationId);
            await ImportResultHelper.UpdateFileStatus(_writeRepoFactory, request.FileDto.FileId, FileStatus.Evaluating);

            if (!result.IsSuccess)
                return Result<string>.Failure(result.Error!, result.ErrorType ?? ErrorType.BadRequest);

            var uploadedFile = await ImportResultHelper.GetResultUploadedFileByFileId(request.FileDto.FileId, _readRepoFactory,
                    cancellationToken);
            if (string.IsNullOrEmpty(uploadedFile.BlobLocation))
            {
                return Result<string>.Failure(
                    "No file found for the specified File ID. Please ensure the file exists and the provided File ID is correct.",
                    ErrorType.BadRequest);
            }

            if (uploadedFile.FileType != null && !IsValidFileType(uploadedFile.FileType))
                return Result<string>.Failure(
                    "The uploaded file format is not supported. Please upload a file in one of the following formats: XLS, XLSX, or JSON.",
                    ErrorType.BadRequest);

            var (fileData, secondTabData) =
                await DownloadAndParseFileAsync(uploadedFile.BlobLocation!,
                    request.FileDto.ConfirmMemberHeaders, cancellationToken);

            var tenantClientId = _utilityService.GetCurrentTenantClientId() ??
                                 throw new Exception("Tenant Client ID not found.");

            await _backgroundTaskQueue.QueueBackgroundWorkItem(
                async (provider, queueToken) =>
                {
                    if (!LongRunningTasks.OperationIds.TryGetValue(operationId, out var externalCts))
                    {
                        externalCts = new CancellationTokenSource();
                        LongRunningTasks.OperationIds.TryAdd(operationId, externalCts);
                    }

                    using var linkedCts =
                        CancellationTokenSource.CreateLinkedTokenSource(queueToken, externalCts.Token);

                    try
                    {
                        await ImportResultDataInBackgroundAsync(
                            provider,
                            uploadedFile,
                            (fileData, secondTabData),
                            request,
                            tenantClientId,
                            request.FileDto.WebSocketId,
                            linkedCts.Token);
                    }
                    finally
                    {
                        if (LongRunningTasks.OperationIds.TryRemove(operationId, out var storedCts))
                        {
                            storedCts.Dispose();
                        }
                    }
                }, CancellationToken.None);

            return operationId;
        }
        catch (CustomValidationException ex)
        {
            return Result<string>.Failure(ex.Message, ErrorType.BadRequest);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"An error occurred while processing the file: {ex.Message}",
                ErrorType.InternalServerError);
        }
    }

    private async Task ImportResultDataInBackgroundAsync(IServiceProvider provider, ResultUploadedFile uploadedFile,
        (List<Dictionary<string, string>> firstTabData, List<Dictionary<string, string>> secondTabData)
            uploadedFileData, ImportResultFileCommand request, string tenantClientId,
        string fileDtoWebSocketId, CancellationToken cancellationToken)
    {
        TenantContextManager.SetTenantClientId(tenantClientId);

        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var readRepoFactory = provider.GetRequiredService<IReadRepositoryFactory>();
        var writeRepoFactory = provider.GetRequiredService<IWriteRepositoryFactory>();
        var progressService = provider.GetRequiredService<IProgressTrackingService>();
        var writeRepo = writeRepoFactory.GetLazyRepository<object>().Value;

        await progressService.SendProgressAsync(fileDtoWebSocketId, "Starting file import", 0, true,
            cancellationToken);
        await progressService.StartPeriodicProgressAsync(cancellationToken);

        DbTransaction? transaction = null;
        var isCommited = false;

        try
        {
            transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

            var memberIdHeader = string.Empty;
            if (uploadedFile.FileType != ".json")
            {
                memberIdHeader = await ImportResultHelper.GetMemberIdColumnNameByScopeId(readRepoFactory, uploadedFile.DisciplineId,
                    cancellationToken);

                if (string.IsNullOrEmpty(memberIdHeader))
                {
                    await progressService.SendProgressAsync(fileDtoWebSocketId,
                        "Member Identifier column mapping not found. Please ensure the Member ID field is mapped correctly.",
                        null, false, cancellationToken);
                    return;
                }
            }

            await progressService.SendProgressAsync(fileDtoWebSocketId, "Processing file data", 5, true,
                cancellationToken);

            var config = await ImportResultHelper.GetResultUploadFieldValidationConfig(readRepoFactory,
                uploadedFile.DisciplineId, 1,
                cancellationToken);

            IFileDataValidator? validator = null;
            if (!string.IsNullOrEmpty(config))
            {
                validator = GetDisciplineFieldValidator(config);
            }

            await progressService.SendProgressAsync(fileDtoWebSocketId, "Processing file data", 15, true,
                cancellationToken);

            await progressService.SendProgressAsync(fileDtoWebSocketId, "Processing file data", 25, true,
                cancellationToken);

            var memberWithDataRecords = ImportResultHelper.ProcessFileDataWithOrder(uploadedFileData.firstTabData,
                memberIdHeader, validator);

            await progressService.SendProgressAsync(fileDtoWebSocketId, "Processing file data", 50, true,
                cancellationToken);

            await ImportResultHelper.BulkInsertUploadedMembersAsync(writeRepo, request.FileDto.FileId,
                memberWithDataRecords,
                transaction,
                cancellationToken);

            await progressService.SendProgressAsync(fileDtoWebSocketId, "Saving file data", 60, true,
                cancellationToken);

            if (uploadedFileData.secondTabData.Count > 0)
            {
                await ImportResultHelper.InsertSecondTabDataAsync(writeRepo, request.FileDto.FileId,
                    uploadedFileData.secondTabData,
                    transaction,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            isCommited = true;

            await progressService.SendProgressAsync(fileDtoWebSocketId, "Saving file data", 70, true,
                cancellationToken);

            await progressService.SendProgressAsync(
                fileDtoWebSocketId,
                "Validating member and horse. Please wait while we ensure all records are accurate and complete.",
                80,
                true,
                cancellationToken);

            await ImportResultHelper.RunValidationAsync(writeRepo, request.FileDto.FileId, uploadedFile.DisciplineId,
                cancellationToken);

            await ImportResultHelper.UpdateFileStatus(writeRepoFactory, request.FileDto.FileId,
                FileStatus.PendingReview);

            await progressService.SendProgressAsync(fileDtoWebSocketId, "File import completed successfully", 100, true,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!isCommited && transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            else
            {
                await ImportResultHelper.DeleteUncommittedFileDataAsync(writeRepo, request.FileDto.FileId,
                    cancellationToken);
            }

            await ImportResultHelper.UpdateFileStatus(writeRepoFactory, request.FileDto.FileId, FileStatus.Failed);

            await progressService.SendProgressAsync(fileDtoWebSocketId,
                "File import operation was cancelled. No changes were made to the database.",
                null,
                false,
                cancellationToken);
        }
        catch (SqlException sqlEx) when (ImportResultHelper.IsCancellationException(sqlEx))
        {
            if (!isCommited && transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            else
            {
                await ImportResultHelper.DeleteUncommittedFileDataAsync(writeRepo, request.FileDto.FileId,
                    cancellationToken);
            }

            await ImportResultHelper.UpdateFileStatus(writeRepoFactory, request.FileDto.FileId, FileStatus.Failed);

            await progressService.SendProgressAsync(fileDtoWebSocketId,
                "File import operation was cancelled. No changes were made to the database.",
                null,
                false,
                cancellationToken);
        }
        catch (CustomValidationException ex)
        {
            if (!isCommited && transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            else
            {
                await ImportResultHelper.DeleteUncommittedFileDataAsync(writeRepo, request.FileDto.FileId,
                    cancellationToken);
            }

            await ImportResultHelper.UpdateFileStatus(writeRepoFactory, request.FileDto.FileId, FileStatus.Failed);

            await progressService.SendProgressAsync(fileDtoWebSocketId,
                $"File import failed due to validation error: {ex.Message}",
                null,
                false,
                cancellationToken);

            CustomLog.Event(AuditScheme.ResultManagement.Value,
                AuditScheme.ResultManagement.ResultUpload.Uploaded.Value,
                0,
                0,
                0,
                EntityType.Result,
                uploadedFile.OwnerId,
                "Created",
                $"ImportResultFileCommand failed; Exception: {ex.Message}; StackTrace: {ex.StackTrace}"
            );
        }
        catch (JsonException ex)
        {
            if (!isCommited && transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            else
            {
                await ImportResultHelper.DeleteUncommittedFileDataAsync(writeRepo, request.FileDto.FileId,
                    cancellationToken);
            }

            await ImportResultHelper.UpdateFileStatus(writeRepoFactory, request.FileDto.FileId, FileStatus.Failed);

            await progressService.SendProgressAsync(fileDtoWebSocketId,
                "File import failed due to JSON parsing error: Invalid JSON format in the uploaded file.",
                null,
                false,
                cancellationToken);

            CustomLog.Event(AuditScheme.ResultManagement.Value,
                AuditScheme.ResultManagement.ResultUpload.Value,
                AuditScheme.ResultManagement.ResultUpload.Confirmed.Value, // Add = 1, Update = 2, Delete = 3
                0, // Current User ID
                0, // ResultEventId -> EventID
                EntityType.Result,
                uploadedFile.OwnerId,
                "Created",
                $"ImportResultFileCommand failed; Exception: {ex.Message}; StackTrace: {ex.StackTrace}"
            );
        }
        catch (Exception ex)
        {
            if (!isCommited && transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            else
            {
                await ImportResultHelper.DeleteUncommittedFileDataAsync(writeRepo, request.FileDto.FileId,
                    cancellationToken);
            }

            await ImportResultHelper.UpdateFileStatus(writeRepoFactory, request.FileDto.FileId, FileStatus.Failed);

            await progressService.SendProgressAsync(fileDtoWebSocketId,
                "An error occurred during file import: " + ex.Message,
                null,
                false,
                cancellationToken);

            CustomLog.Event(AuditScheme.ResultManagement.Value,
                AuditScheme.ResultManagement.ResultUpload.Uploaded.Value,
                0,
                0,
                0,
                EntityType.Result,
                uploadedFile.OwnerId,
                "Created",
                $"ImportResultFileCommand failed; Exception: {ex.Message}; StackTrace: {ex.StackTrace}"
            );
        }
        finally
        {
            progressService.StopPeriodicProgress();
            TenantContextManager.ClearTenantClientId();
        }
    }

    private IFileDataValidator GetDisciplineFieldValidator(string config)
    {
        return new FileDataValidator(config);
    }

    private static bool IsValidFileType(string fileType)
    {
        var baseTypes = new[] { ".xlsx", ".xls", ".json" };
        return baseTypes.Contains(fileType);
    }

    private async Task<(List<Dictionary<string, string>>, List<Dictionary<string, string>>)> DownloadAndParseFileAsync(
        string location, List<ConfirmMemberDataDto> confirmMemberHeaders,
        CancellationToken cancellationToken)
    {
        var blobClient = await _azureBlobFileService.GetBolbClientAsync(location, cancellationToken);

        if (blobClient is null)
        {
            throw new CustomValidationException("File not found in blob storage.");
        }

        var fileName = Path.GetFileName(location);
        var fileType = Path.GetExtension(fileName).ToLower();

        await using var fileStream = await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
        List<Dictionary<string, string>> fileData = [];
        List<Dictionary<string, string>> secondTabData = [];

        switch (fileType)
        {
            case ".json":
                await HandleEventingJsonFile.ParseJsonFileAsync(fileStream, fileData, cancellationToken);
                break;
            case ".xls":
            case ".xlsx":
                ImportResultHelper.ParseXlsFile(fileStream, confirmMemberHeaders, fileData, secondTabData);
                break;
            default:
                throw new CustomValidationException(
                    "Unsupported file format. Only JSON and XLS or XLSX files are supported.");
        }

        return (fileData, secondTabData);
    }
}