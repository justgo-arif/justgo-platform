using System.Data.Common;
using System.Text.Json;
using Dapper;
using ExcelDataReader;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Helper.Enums;
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
using JustGo.Result.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands.ImportTableTennisResult;

public class TableTennisResultProcessor : IResultProcessor
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReadRepositoryFactory _readRepoFactory;
    private readonly IUtilityService _utilityService;
    private readonly IAzureBlobFileService _azureBlobFileService;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;

    public TableTennisResultProcessor(IAzureBlobFileService azureBlobFileService, IUtilityService utilityService,
        IUnitOfWork unitOfWork, IReadRepositoryFactory readRepoFactory,
        IBackgroundTaskQueue backgroundTaskQueue, IWriteRepositoryFactory writeRepositoryFactory)
    {
        _azureBlobFileService = azureBlobFileService;
        _utilityService = utilityService;
        _unitOfWork = unitOfWork;
        _readRepoFactory = readRepoFactory;
        _backgroundTaskQueue = backgroundTaskQueue;
        _writeRepositoryFactory = writeRepositoryFactory;
    }

    public async Task<Result<string>> ProcessAsync(ImportResultFileCommand request, CancellationToken cancellationToken)
    {
        var operationId = Guid.NewGuid().ToString();
        await ImportResultHelper.UpdateOperationIdInUploadedFile(_writeRepositoryFactory, request.FileDto.FileId, operationId);
        await ImportResultHelper.UpdateFileStatus(_writeRepositoryFactory, request.FileDto.FileId, FileStatus.Evaluating);
        
        var fieldsCount = await ImportResultHelper.GetCompulsoryFieldsCountAsync(
            _readRepoFactory.GetRepository<object>(),
            request.FileDto.FileId, 1, cancellationToken);

        if (request.FileDto.ConfirmMemberHeaders.Select(x => x.IsMapped).Count(y => y) != fieldsCount)
        {
            return Result<string>.Failure(
                "The required compulsory fields are not mapped correctly. Please ensure all mandatory fields are properly mapped.",
                ErrorType.BadRequest);
        }

        var uploadedFile = await GetResultUploadedFileByFileId(request.FileDto.FileId, cancellationToken);
        if (string.IsNullOrEmpty(uploadedFile.BlobLocation))
        {
            return Result<string>.Failure("No File found for the specific File Id", ErrorType.BadRequest);
        }

        var (firstTabData, secondTabData) = await ParseFileFromBlobByLocationAsync(
            uploadedFile.BlobLocation,
            request.FileDto.ConfirmMemberHeaders,
            cancellationToken);
        
        var tenantClientId = _utilityService.GetCurrentTenantClientId() ?? throw new CustomValidationException(
            "Tenant Client Id not found.");

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
                        (firstTabData, secondTabData),
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

    private async Task ImportResultDataInBackgroundAsync(IServiceProvider provider, ResultUploadedFile uploadedFile,
        (List<Dictionary<string, string>> firstTabData, List<Dictionary<string, string>> secondTabData)
            uploadedFileData, ImportResultFileCommand request, string tenantClientId,
        string fileDtoWebSocketId, CancellationToken cancellationToken)
    {
        TenantContextManager.SetTenantClientId(tenantClientId);
        
        var writeRepositoryFactory = provider.GetRequiredService<IWriteRepositoryFactory>();
        var utilityService = provider.GetRequiredService<IUtilityService>();
        var progressService = provider.GetRequiredService<IProgressTrackingService>();
        var writeRepo = writeRepositoryFactory.GetLazyRepository<object>().Value;
        
        await progressService.SendProgressAsync(fileDtoWebSocketId,
            "Starting the import of table tennis result data", 0, true, cancellationToken);
        await progressService.StartPeriodicProgressAsync(cancellationToken);
        
        var isAlreadyCommitted = false;
        DbTransaction? transaction = null;

        try
        {
            transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            
            await progressService.SendProgressAsync(fileDtoWebSocketId,
                "Importing table tennis result data: Inserting main data", 20, true, cancellationToken);
            
            var memberWithDataRecords = ProcessFileDataWithOrder(uploadedFileData.firstTabData);
            
            await progressService.SendProgressAsync(fileDtoWebSocketId,
                "Importing table tennis result data: Inserting main data - Bulk inserting records", 30, true,
                cancellationToken);
            
            await ImportResultHelper.BulkInsertUploadedMembersAsync(writeRepo, request.FileDto.FileId, memberWithDataRecords,
                transaction,
                cancellationToken);
            
            await progressService.SendProgressAsync(fileDtoWebSocketId,
                "Importing table tennis result data: Inserting second tab data", 40, true, cancellationToken);

            await ImportResultHelper.InsertSecondTabDataAsync(writeRepo, request.FileDto.FileId, uploadedFileData.secondTabData,
                transaction,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            isAlreadyCommitted = true;
            
            await progressService.SendProgressAsync(fileDtoWebSocketId,
                "Finalizing table tennis result data import: Validating imported data", 60, true, cancellationToken);

            await ImportResultHelper.RunValidationAsync(writeRepo, request.FileDto.FileId, uploadedFile.DisciplineId,
                cancellationToken);
            
            await ImportResultHelper.UpdateFileStatus(writeRepositoryFactory, request.FileDto.FileId, FileStatus.PendingReview);
            
            await progressService.SendProgressAsync(fileDtoWebSocketId,
                "Finalizing table tennis result data import: Validation completed", 98, true, cancellationToken);
            
            await progressService.SendProgressAsync(fileDtoWebSocketId,
                "Result data import completed successfully", 100, true, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!isAlreadyCommitted && transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            else
            {
                await ImportResultHelper.DeleteUncommittedFileDataAsync(writeRepo, request.FileDto.FileId,
                    cancellationToken);
            }
            
            await ImportResultHelper.UpdateFileStatus(writeRepositoryFactory, request.FileDto.FileId, FileStatus.Failed);

            await progressService.SendProgressAsync(fileDtoWebSocketId,
                "File import operation was cancelled. No changes were made to the database.",
                100,
                false,
                cancellationToken);
        }
        catch (SqlException sqlEx) when (ImportResultHelper.IsCancellationException(sqlEx))
        {
            if (!isAlreadyCommitted && transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            else
            {
                await ImportResultHelper.DeleteUncommittedFileDataAsync(writeRepo, request.FileDto.FileId,
                    cancellationToken);
            }
            
            await ImportResultHelper.UpdateFileStatus(writeRepositoryFactory, request.FileDto.FileId, FileStatus.Failed);

            await progressService.SendProgressAsync(fileDtoWebSocketId,
                "File import operation was cancelled. No changes were made to the database.",
                100,
                false,
                cancellationToken);
        }
        catch (CustomValidationException ex)
        {
            if (!isAlreadyCommitted && transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            else
            {
                await ImportResultHelper.DeleteUncommittedFileDataAsync(writeRepo, request.FileDto.FileId, 
                    cancellationToken);
            }
            
            await ImportResultHelper.UpdateFileStatus(writeRepositoryFactory, request.FileDto.FileId, FileStatus.Failed);
            
            await progressService.SendProgressAsync(fileDtoWebSocketId,
                "File import failed due to validation errors: " + ex.Message,
                100,
                false,
                cancellationToken);
        }
        catch (Exception ex)
        {
            if (!isAlreadyCommitted && transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            else
            {
                await ImportResultHelper.DeleteUncommittedFileDataAsync(writeRepo, request.FileDto.FileId, 
                    cancellationToken);
            }
            
            await ImportResultHelper.UpdateFileStatus(writeRepositoryFactory, request.FileDto.FileId, FileStatus.Failed);
            
            await progressService.SendProgressAsync(fileDtoWebSocketId,
                "An error occurred during file import: " + ex.Message,
                100,
                false,
                cancellationToken);
        }
        finally
        { 
            progressService.StopPeriodicProgress();
            TenantContextManager.ClearTenantClientId();
        }
    }

    private async Task<ResultUploadedFile> GetResultUploadedFileByFileId(int fileId,
        CancellationToken cancellationToken)
    {
        const string getResultUploadedFileQuery = """
                                                  SELECT [UploadedFileId],
                                                         [FileType],
                                                         [UpdatedBy],
                                                         [UploadedAt],
                                                         [FileName],
                                                         [Notes],
                                                         [IsFinal],
                                                         [IsDeleted],
                                                         [OwnerId],
                                                         [DisciplineId],
                                                         [EventId],
                                                         [FileCategory],
                                                         [CompetitionStatusId],
                                                         [BlobLocation]
                                                  FROM [dbo].[ResultUploadedFile]
                                                  WHERE [IsDeleted] = 0 AND [UploadedFileId] = @UploadedFileId;
                                                  """;
        var queryParameters = new DynamicParameters();
        queryParameters.Add("@UploadedFileId", fileId);
        var repo = _readRepoFactory.GetRepository<ResultUploadedFile>();
        var fileData = await repo.QueryFirstAsync<ResultUploadedFile>(getResultUploadedFileQuery, queryParameters, null,
            QueryType.Text, cancellationToken);

        return fileData ?? throw new CustomValidationException("Uploaded file not found.");
    }

    private async Task<(List<Dictionary<string, string>>, List<Dictionary<string, string>>)>
        ParseFileFromBlobByLocationAsync(string location, List<ConfirmMemberDataDto> confirmMemberHeaders,
            CancellationToken cancellationToken)
    {
        var blobClient = await _azureBlobFileService.GetBolbClientAsync(location, cancellationToken);

        if (blobClient is null)
        {
            throw new CustomValidationException("File not found in blob storage.");
        }

        await using var fileStream = await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
        List<Dictionary<string, string>> fileData = [];
        List<Dictionary<string, string>> secondTabData = [];

        await Task.Run(() => ParseExcelFileAsync(fileStream, confirmMemberHeaders, fileData, secondTabData),
            cancellationToken);

        return (fileData, secondTabData);
    }

    private static void ParseExcelFileAsync(
        Stream memoryStream,
        List<ConfirmMemberDataDto> confirmMemberDataDto,
        List<Dictionary<string, string>> fileData,
        List<Dictionary<string, string>> secondTabData)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        using var reader = ExcelReaderFactory.CreateReader(memoryStream);
        var result = reader.AsDataSet();

        if (result.Tables.Count == 0)
            throw new CustomValidationException(
                "The uploaded Excel file does not contain any worksheets. Please ensure the file is not empty and has at least one worksheet.");

        // --------------------------
        // Parse 1st tab (main data)
        // --------------------------
        var mainTable = result.Tables[0];
        var mainHeaders = Enumerable.Range(0, mainTable.Columns.Count)
            .Select(i => mainTable.Rows[0][i]?.ToString() ?? $"Column{i}")
            .ToList();

        mainHeaders = mainHeaders.Select(x =>
        {
            var mapping = confirmMemberDataDto
                .FirstOrDefault(c => c.FileHeaderName.Equals(x, StringComparison.OrdinalIgnoreCase) && c.IsMapped);
            return mapping != null ? mapping.SystemColumnName : x;
        }).ToList();

        for (int row = 1; row < mainTable.Rows.Count; row++)
        {
            var rowDict = new Dictionary<string, string>();

            for (int col = 0; col < mainTable.Columns.Count; col++)
            {
                rowDict[mainHeaders[col]] = mainTable.Rows[row][col]?.ToString() ?? string.Empty;
            }

            fileData.Add(rowDict);
        }

        if (fileData.Count == 0)
        {
            throw new CustomValidationException(
                "The first sheet of the uploaded Excel file contains no data rows. Please ensure the file is not empty and the first sheet has valid data rows.");
        }

        // --------------------------
        // Parse 2nd tab (Officials)
        // --------------------------
        if (result.Tables.Count <= 1) return;
        {
            var table = result.Tables[1];
            var officialHeaders = Enumerable.Range(0, table.Columns.Count)
                .Select(i => table.Rows[0][i].ToString()?.Trim() ?? $"Column{i}")
                .ToList();

            for (int row = 1; row < table.Rows.Count; row++)
            {
                var rowDict = new Dictionary<string, string>();
                for (int col = 0; col < table.Columns.Count; col++)
                {
                    rowDict[officialHeaders[col]] = table.Rows[row][col]?.ToString() ?? string.Empty;
                }

                secondTabData.Add(rowDict);
            }

            // var json = JsonSerializer.Serialize(officials, ImportResultHelper.JsonOptions);
            // foreach (var mainRow in fileData)
            // {
            //     mainRow[secondTabName ?? "Estimated Ratings"] = json;
            // }
        }
    }

    private static List<MemberWithDataRecord> ProcessFileDataWithOrder(
        List<Dictionary<string, string>> fileData)
    {
        var memberWithDataRecords = new List<MemberWithDataRecord>();

        for (var index = 0; index < fileData.Count; index++)
        {
            var row = fileData[index];

            var additionalData = row
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var memberDataJson = JsonSerializer.Serialize(additionalData, ImportResultHelper.JsonOptions);

            memberWithDataRecords.Add(new MemberWithDataRecord
            {
                MemberData = memberDataJson,
                RowOrder = index
            });
        }

        return memberWithDataRecords;
    }
}