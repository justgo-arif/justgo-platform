using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using CsvHelper;
using Dapper;
using ExcelDataReader;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.HostedServices;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.FileSystemManager.AzureBlob;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SignalR;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.Common.Enums;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;
using JustGo.Result.Application.Features.MemberUpload.Helpers;
using JustGo.Result.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.ImportMemberCommands
{
    public class ImportMemberCommandHandler : IRequestHandler<ImportMemberCommand, Result<string>>
    {
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IAzureBlobFileService _azureBlobFileService;
        private readonly IUtilityService _utilityService;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;

        public ImportMemberCommandHandler(IWriteRepositoryFactory writeRepoFactory,
            IReadRepositoryFactory readRepository,
            IAzureBlobFileService azureBlobFileService,
            IUtilityService utilityService,
            IBackgroundTaskQueue backgroundTaskQueue)
        {
            _writeRepository = writeRepoFactory;
            _readRepository = readRepository;
            _azureBlobFileService = azureBlobFileService;
            _utilityService = utilityService;
            _backgroundTaskQueue = backgroundTaskQueue;
        }

        public async Task<Result<string>> Handle(ImportMemberCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await UpdateFileStatus(_writeRepository, request.FileDto.FileId, FileStatus.Evaluating);
                await UpdateOperationIdInUploadedFile(_writeRepository, request.FileDto.FileId, request.OperationId);

                var uploadedFile = await GetResultUploadedFileByFileId(request.FileDto.FileId, cancellationToken);
                if (string.IsNullOrEmpty(uploadedFile.BlobLocation))
                {
                    return Result<string>.Failure("No File Location found", ErrorType.BadRequest);
                }

                var (fileData, headers) = await ParseFileFromBlobByLocationAsync(
                    uploadedFile.BlobLocation,
                    request.FileDto.ConfirmMemberHeaders,
                    cancellationToken);

                var (isValid, message) = await VerifyHeaders(headers, uploadedFile.DisciplineId, cancellationToken);
                if (!isValid)
                {
                    return Result<string>.Failure(message, ErrorType.BadRequest);
                }

                var tenantClientId = _utilityService.GetCurrentTenantClientId() ??
                                     throw new Exception("Tenant information is missing. Please contact support or try again later.");

                await _backgroundTaskQueue.QueueBackgroundWorkItem(async (serviceProvider, queueToken) =>
                {
                    if (!LongRunningTasks.OperationIds.TryGetValue(request.OperationId, out var externalCts))
                    {
                        externalCts = new CancellationTokenSource();
                        LongRunningTasks.OperationIds.TryAdd(request.OperationId, externalCts);
                    }

                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(queueToken, externalCts.Token);
                    linkedCts.CancelAfter(TimeSpan.FromMinutes(90));
                    
                    try
                    {
                        await ExecuteBackgroundMemberValidationAsync(
                            serviceProvider,
                            request,
                            uploadedFile,
                            fileData,
                            tenantClientId,
                            linkedCts.Token);
                    }
                    finally
                    {
                        if (LongRunningTasks.OperationIds.TryRemove(request.OperationId, out var storedCts))
                        {
                            storedCts.Dispose();
                        }
                    }
                }, CancellationToken.None);

                return request.OperationId;
            }
            catch (Exception ex)
            {
                await UpdateErrorMessageAndStatusInUploadedFile(
                    _writeRepository,
                    request.FileDto.FileId,
                    FileStatus.Failed,
                    $"File processing failed: {ex.Message}.");
                
                CustomLog.Event(AuditScheme.ResultManagement.Value,
                    AuditScheme.ResultManagement.EntryValidation.ImportMembers.Value,
                    0,
                    0,
                    0,
                    EntityType.Result,
                    request.FileDto.FileId,
                    "Created",
                    $"ImportMemberCommand failed; Exception: {ex.Message}; StackTrace: {ex.StackTrace}"
                );

                return Result<string>.Failure(
                    "An error occurred while initiating member import. Please try again.",
                    ErrorType.InternalServerError);
            }
        }

        private async Task ExecuteBackgroundMemberValidationAsync(
            IServiceProvider serviceProvider,
            ImportMemberCommand request,
            ResultUploadedFile uploadedFile,
            List<Dictionary<string, string>> fileData,
            string tenantClientId,
            CancellationToken cancellationToken)
        {
            TenantContextManager.SetTenantClientId(tenantClientId);

            var scopedWriteRepository = serviceProvider.GetRequiredService<IWriteRepositoryFactory>();
            var scopedReadRepository = serviceProvider.GetRequiredService<IReadRepositoryFactory>();
            var scopedUnitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
            var progressService = serviceProvider.GetRequiredService<IProgressTrackingService>();

            DbTransaction? transaction = null;
            var isAlreadyCommitted = false;

            try
            {
                transaction = await scopedUnitOfWork.BeginTransactionAsync(cancellationToken);
                
                await progressService.SendProgressAsync(request.FileDto.WebSocketId, "Starting file processing", 0,
                    cancellationToken: cancellationToken);
                await progressService.StartPeriodicProgressAsync(cancellationToken);

                await ProcessMemberValidationAsync(
                    request,
                    uploadedFile,
                    fileData,
                    scopedWriteRepository,
                    scopedReadRepository,
                    transaction,
                    progressService,
                    cancellationToken);

                await progressService.SendProgressAsync(request.FileDto.WebSocketId, "Saving changes", 50,
                    cancellationToken: cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                
                isAlreadyCommitted = true;
                await progressService.SendProgressAsync(request.FileDto.WebSocketId, "Resolving additional fields", 55,
                    cancellationToken: cancellationToken);
                await ResolveDisciplineWiseHelperColumns(scopedWriteRepository,
                    uploadedFile.UploadedFileId, cancellationToken);

                await UpdateFileStatus(scopedWriteRepository, request.FileDto.FileId, FileStatus.PendingReview);
                await progressService.SendProgressAsync(request.FileDto.WebSocketId,
                    "File processing completed successfully",
                    100, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await progressService.SendProgressAsync(request.FileDto.WebSocketId, "Operation cancelled by user", 100,
                    false, CancellationToken.None);

                if (transaction is not null)
                    await HandleCancellation(scopedWriteRepository, transaction, isAlreadyCommitted,
                        request.FileDto.FileId);

                await UpdateFileStatus(scopedWriteRepository, request.FileDto.FileId, FileStatus.Cancelled);
            }
            catch (SqlException sqlEx) when (IsCancellationException(sqlEx))
            {
                await progressService.SendProgressAsync(request.FileDto.WebSocketId, "Operation cancelled by user", 100,
                    false, CancellationToken.None);

                if (transaction is not null)
                    await HandleCancellation(scopedWriteRepository, transaction, isAlreadyCommitted,
                        request.FileDto.FileId);

                await UpdateFileStatus(scopedWriteRepository, request.FileDto.FileId, FileStatus.Cancelled);
            }
            catch (CustomValidationException ex)
            {
                await progressService.SendProgressAsync(request.FileDto.WebSocketId, $"Validation failed: {ex.Message}",
                    100,
                    false, CancellationToken.None);

                if (!isAlreadyCommitted && transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                await UpdateErrorMessageAndStatusInUploadedFile(scopedWriteRepository, request.FileDto.FileId,
                    FileStatus.Failed,
                    ex.Message);
            }
            catch (Exception ex)
            {
                await progressService.SendProgressAsync(request.FileDto.WebSocketId,
                    $"Processing failed due to unexpected error, {ex.Message}", 100, false, CancellationToken.None);

                if (!isAlreadyCommitted && transaction is not null)
                    await transaction.RollbackAsync(cancellationToken);

                await UpdateErrorMessageAndStatusInUploadedFile(
                    scopedWriteRepository,
                    request.FileDto.FileId,
                    FileStatus.Failed,
                    $"File processing failed: {ex.Message}.");

                CustomLog.Event(AuditScheme.ResultManagement.Value,
                    AuditScheme.ResultManagement.EntryValidation.ImportMembers.Value,
                    0,
                    0,
                    0,
                    EntityType.Result,
                    request.FileDto.FileId,
                    "Created",
                    $"ImportResultFileCommand failed; Exception: {ex.Message}; StackTrace: {ex.StackTrace}"
                );
            }
            finally
            {
                progressService.StopPeriodicProgress();
                await UpdateOperationIdInUploadedFile(scopedWriteRepository, request.FileDto.FileId, null);

                try
                {
                    await SendEmailToUser(scopedWriteRepository, request.FileDto.FileId,
                        cancellationToken);
                }
                catch (Exception emailEx)
                {
                    CustomLog.Event(AuditScheme.ResultManagement.Value,
                        AuditScheme.ResultManagement.EntryValidation.ImportMembers.Value,
                        0,
                        0,
                        0,
                        EntityType.Result,
                        request.FileDto.FileId,
                        "Created",
                        $"ImportResultFileCommand failed; Exception: {emailEx.Message}; StackTrace: {emailEx.StackTrace}"
                    );
                }

                TenantContextManager.ClearTenantClientId();
            }
        }

        private static async Task UpdateOperationIdInUploadedFile(IWriteRepositoryFactory writeRepositoryFactory,
            int fileId, string? operationId)
        {
            var repo = writeRepositoryFactory.GetRepository<object>();
            const string updateQuery = """
                                       UPDATE ResultUploadedFile
                                       SET CurrentProcessId = @OperationId
                                       WHERE UploadedFileId = @UploadedFileId;
                                       """;
            var parameters = new DynamicParameters();
            parameters.Add("@OperationId", operationId ?? null, DbType.String);
            parameters.Add("@UploadedFileId", fileId, DbType.Int32);

            await repo.ExecuteAsync(updateQuery, CancellationToken.None,
                parameters, null, QueryType.Text);
        }

        private static async Task HandleCancellation(IWriteRepositoryFactory writeRepositoryFactory,
            DbTransaction transaction,
            bool isAlreadyCommitted, int fileId)
        {
            if (!isAlreadyCommitted)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            else
            {
                await DeleteFileDetailsWhenCancelled(writeRepositoryFactory, fileId);
            }
        }

        private static async Task UpdateErrorMessageAndStatusInUploadedFile(
            IWriteRepositoryFactory writeRepositoryFactory,
            int fileId, FileStatus fileStatus,
            string errorMessage)
        {
            var repo = writeRepositoryFactory.GetRepository<object>();
            const string updateQuery = """
                                       UPDATE ResultUploadedFile
                                       SET ErrorMessage = @ErrorMessage
                                       WHERE UploadedFileId = @UploadedFileId;

                                       UPDATE ResultUploadedFile
                                       SET FileStatusId = @FileStatusId
                                       WHERE UploadedFileId = @UploadedFileId;
                                       """;
            var parameters = new DynamicParameters();
            parameters.Add("@ErrorMessage", errorMessage, DbType.String);
            parameters.Add("@UploadedFileId", fileId, DbType.Int32);
            parameters.Add("@FileStatusId", (int)fileStatus, DbType.Int32);

            await repo.ExecuteAsync(updateQuery, CancellationToken.None,
                parameters, null, QueryType.Text);
        }

        private static async Task UpdateFileStatus(IWriteRepositoryFactory writeRepositoryFactory, int fileId,
            FileStatus fileStatus)
        {
            var repo = writeRepositoryFactory.GetRepository<object>();
            const string updateQuery = """
                                       UPDATE ResultUploadedFile
                                       SET FileStatusId = @FileStatusId
                                       WHERE UploadedFileId = @UploadedFileId;
                                       """;

            var parameters = new DynamicParameters();
            parameters.Add("@UploadedFileId", fileId, DbType.Int32);
            parameters.Add("@FileStatusId", (int)fileStatus, DbType.Int32);

            await repo.ExecuteAsync(updateQuery, CancellationToken.None,
                parameters, null, QueryType.Text);
        }

        private static bool IsCancellationException(SqlException sqlException)
        {
            return sqlException.Number == 0
                   || sqlException.Number == -2
                   || sqlException.Message.Contains("Operation cancelled by user", StringComparison.OrdinalIgnoreCase)
                   || sqlException.Message.Contains("Timeout", StringComparison.OrdinalIgnoreCase);

        }

        private static async Task DeleteFileDetailsWhenCancelled(IWriteRepositoryFactory writeRepositoryFactory,
            int fileId)
        {
            var repo = writeRepositoryFactory.GetRepository<object>();
            const string deleteQuery = """
                                       DELETE md
                                       FROM ResultUploadedMemberData md
                                       INNER JOIN ResultUploadedMember m ON md.UploadedMemberId = m.UploadedMemberId
                                       WHERE m.UploadedFileId = @UploadedFileId;
                                              
                                       DELETE FROM ResultUploadedMember WHERE UploadedFileId = @UploadedFileId;
                                       """;
            var parameters = new DynamicParameters();
            parameters.Add("@UploadedFileId", fileId, DbType.Int32);

            await repo.ExecuteAsync(deleteQuery, CancellationToken.None,
                parameters, null, QueryType.Text);
        }

        private static async Task ResolveDisciplineWiseHelperColumns(
            IWriteRepositoryFactory writeRepositoryFactory, int fileId,
            CancellationToken cancellationToken)
        {
            var repo = writeRepositoryFactory.GetRepository<object>();

            var parameters = new DynamicParameters();
            parameters.Add("@FileId", fileId, DbType.Int32);

            await repo.ExecuteUnboundedAsync("ResolveDisciplineWiseHelperColumns", cancellationToken,
                parameters, null, QueryType.StoredProcedure);
        }

        private static async Task SaveConfirmHeaderMapping(IWriteRepositoryFactory writeRepositoryFactory,
            ConfirmMemberFileDto requestFileDto, IDbTransaction transaction,
            CancellationToken cancellationToken)
        {
            const string insertConfirmHeaderMappingQuery = """
                                                           BEGIN
                                                               INSERT INTO ResultUploadConfirmHeaderMapping (
                                                                   UploadedFileId,
                                                                   SystemHeaderName,
                                                                   MappedHeaderName,
                                                                   IsMapped
                                                               )
                                                               VALUES (
                                                                   @UploadedFileId,
                                                                   @SystemHeaderName,
                                                                   @MappedHeaderName,
                                                                   @IsMapped
                                                               );
                                                           END
                                                           """;
            var mappedHeaders = new List<ResultUploadConfirmHeaderMapping>();
            var repo = writeRepositoryFactory.GetLazyRepository<object>().Value;
            int fileId = requestFileDto.FileId;
            foreach (var item in requestFileDto.ConfirmMemberHeaders)
            {
                var mappedHeader = new ResultUploadConfirmHeaderMapping
                {
                    UploadedFileId = fileId,
                    SystemHeaderName = item.SystemColumnName,
                    MappedHeaderName = item.FileHeaderName,
                    IsMapped = item.IsMapped
                };

                mappedHeaders.Add(mappedHeader);
            }

            await repo.ExecuteAsync(insertConfirmHeaderMappingQuery, cancellationToken, mappedHeaders, transaction,
                QueryType.Text);
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
            var repo = _readRepository.GetRepository<ResultUploadedFile>();
            return (await repo.QueryFirstAsync<ResultUploadedFile>(getResultUploadedFileQuery, queryParameters, null,
                QueryType.Text, cancellationToken))!;
        }

        private static List<string> ProcessFiles(string fileType, List<ConfirmMemberDataDto> confirmMemberDataDto,
            Stream stream, List<Dictionary<string, string>> fileData)
        {
            var headers = fileType switch
            {
                FileExtensionType.CSV => ProcessCsvFile(stream, confirmMemberDataDto, fileData),
                FileExtensionType.EXCEL or FileExtensionType.EXCEL_OLD => ProcessExcelFile(stream, confirmMemberDataDto,
                    fileData),
                _ => throw new CustomValidationException(
                    "Unsupported file format. Please upload a .csv, .xls, or .xlsx file.")
            };

            return headers;
        }
        
        private static List<string> ProcessCsvFile(Stream stream, List<ConfirmMemberDataDto> confirmMemberDataDto, List<Dictionary<string, string>> fileData)
        {
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();
            
            List<string> originalHeaders = [];
            
            if (csv.HeaderRecord != null)
            {
                int lastValidHeaderIndex = -1;
                for (int i = csv.HeaderRecord.Length - 1; i >= 0; i--)
                {
                    if (!string.IsNullOrWhiteSpace(csv.HeaderRecord[i]))
                    {
                        lastValidHeaderIndex = i;
                        break;
                    }
                }
                
                for (int idx = 0; idx <= lastValidHeaderIndex; idx++)
                {
                    var header = csv.HeaderRecord[idx];
                    if (string.IsNullOrWhiteSpace(header))
                    {
                        throw new InvalidOperationException(
                            $"Column {idx + 1} header is missing in the CSV file. Please ensure all columns have headers.");
                    }
                }
                
                originalHeaders = csv.HeaderRecord.Take(lastValidHeaderIndex + 1).ToList();
            }
            
            if (originalHeaders.Count == 0)
            {
                return [];
            }

            var headerMapping = confirmMemberDataDto
                .Where(c => c.IsMapped)
                .ToDictionary(
                    c => c.FileHeaderName, 
                    c => c.SystemColumnName, 
                    StringComparer.OrdinalIgnoreCase);

            var mappedHeaders = new string[originalHeaders.Count];
            for (var i = 0; i < originalHeaders.Count; i++)
            {
                var originalHeader = originalHeaders[i];
                mappedHeaders[i] = headerMapping.TryGetValue(originalHeader, out var systemHeader) 
                    ? systemHeader 
                    : originalHeader;
            }

            var columnCount = originalHeaders.Count;

            while (csv.Read())
            {
                var recordDict = new Dictionary<string, string>(columnCount);

                for (var i = 0; i < columnCount; i++)
                {
                    var cellValue = csv.GetField(i)?.Trim() ?? string.Empty;
                    recordDict[mappedHeaders[i]] = cellValue;
                }

                fileData.Add(recordDict);
            }

            return mappedHeaders.ToList();
        }
        
        private static List<string> ProcessExcelFile(Stream stream, List<ConfirmMemberDataDto> confirmMemberDataDto,
            List<Dictionary<string, string>> fileData)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var result = reader.AsDataSet();

            var table = result.Tables[0];
            var headers = Enumerable.Range(0, table.Columns.Count)
                .Select(i => table.Rows[0][i]?.ToString()?.Trim() ?? $"Column{i}")
                .ToList();
            
            headers = headers.Select(x =>
            {
                var mapping = confirmMemberDataDto
                    .FirstOrDefault(c => c.FileHeaderName.Equals(x, StringComparison.OrdinalIgnoreCase) && c.IsMapped);
                return mapping != null ? mapping.SystemColumnName : x;
            }).ToList();

            for (var row = 1; row < table.Rows.Count; row++)
            {
                var rowDict = new Dictionary<string, string>();
                for (var col = 0; col < table.Columns.Count; col++)
                {
                    rowDict[headers[col]] = table.Rows[row][col]?.ToString()?.Trim() ?? string.Empty;
                }

                fileData.Add(rowDict);
            }
            
            if (fileData.Count == 0)
            {
                throw new CustomValidationException("Excel file contains no data rows in first sheet.");
            }

            return headers;
        }
        
        private async Task<(List<Dictionary<string, string>> fileData, List<string> headers)>
            ParseFileFromBlobByLocationAsync(string location, List<ConfirmMemberDataDto> confirmMemberHeaders,
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

            var headers = await Task.Run(() => ProcessFiles(fileType, confirmMemberHeaders, fileStream, fileData),
                cancellationToken);
            return (fileData, headers);
        }

        private async Task UpdateResultUploadFile(IWriteRepositoryFactory writeRepositoryFactory,
            ResultUploadedFile uploadedFile,
            DbTransaction transaction,
            CancellationToken cancellationToken)
        {
            var (updateDataSql, updateDataParams) = SQLHelper.GenerateUpdateSQLWithParameters(
                uploadedFile,
                "UploadedFileId",
                [
                    "FileType", "UpdatedBy", "UploadedAt", "FileName", "Notes", "IsDeleted", "OwnerId", "DisciplineId",
                    "EventId", "FileCategory", "CompetitionStatusId", "BlobLocation"
                ],
                tableName: "ResultUploadedFile");

            var dataRepo = writeRepositoryFactory.GetLazyRepository<ResultUploadedFile>().Value;

            await dataRepo.ExecuteAsync(updateDataSql, cancellationToken, updateDataParams, transaction,
                QueryType.Text);
        }

        private static bool IsRowEmpty(Dictionary<string, string> row)
        {
            return row.Values.All(string.IsNullOrWhiteSpace);
        }

        private static async Task<DataTable> ProcessRows
        (IReadRepositoryFactory readRepositoryFactory,
            List<Dictionary<string, string>> fileData, int validationScopeId, string memberIdHeader,
            CancellationToken cancellationToken)
        {
            var resolvedValidationScopeDependencies = await MemberUploadHelper.ResolveValidationScopeDependency(
                readRepositoryFactory,
                validationScopeId, cancellationToken);

            var table = new DataTable();
            table.Columns.Add("TargetValidationScopeId", typeof(int));
            table.Columns.Add("MemberId", typeof(string));
            table.Columns.Add("MemberIdHeader", typeof(string));
            table.Columns.Add("ShouldResolveValidationScope", typeof(bool));
            table.Columns.Add("MemberDataJson", typeof(string));

            foreach (var row in fileData)
            {
                if (IsRowEmpty(row))
                {
                    continue;
                }

                var targetValidationScopeId = -1;
                if (resolvedValidationScopeDependencies.ShouldResolveValidationScope)
                    MemberUploadHelper.ResolveValidationScopeId(row,
                        resolvedValidationScopeDependencies.ValidationScopeFieldMappings,
                        resolvedValidationScopeDependencies.HeaderName, ref targetValidationScopeId);

                targetValidationScopeId = (!resolvedValidationScopeDependencies.ShouldResolveValidationScope &&
                                           targetValidationScopeId == -1)
                    ? validationScopeId
                    : targetValidationScopeId;

                if (string.IsNullOrEmpty(memberIdHeader))
                {
                    memberIdHeader =
                        resolvedValidationScopeDependencies.ValidatedMemberIdHeaders
                            .FirstOrDefault(m => m.ValidationScopeId == targetValidationScopeId)
                            .ValidationItemDisplayName;
                }

                var memberId = !string.IsNullOrEmpty(memberIdHeader) ? row[memberIdHeader]?.Trim() : string.Empty;

                var memberDataJson = JsonSerializer.Serialize(row);

                table.Rows.Add(targetValidationScopeId, memberId, memberIdHeader,
                    resolvedValidationScopeDependencies.ShouldResolveValidationScope, memberDataJson);
            }

            return table;
        }

        private static async Task SaveUploadedFileDataAsync(IWriteRepositoryFactory writeRepositoryFactory,
            DataTable dataTable, int uploadedFileId,
            IDbTransaction transaction, CancellationToken cancellationToken)
        {
            var repo = writeRepositoryFactory.GetLazyRepository<object>().Value;

            var parameters = new DynamicParameters();
            parameters.Add("@UploadedFileId", uploadedFileId, DbType.Int32);
            parameters.Add("@MemberDataRecords",
                dataTable.AsTableValuedParameter("dbo.ResultUploadedMemberProcessRow"));

            await repo.ExecuteUnboundedAsync("ProcessMemberDataRows", cancellationToken,
                parameters, transaction, QueryType.StoredProcedure);
        }

        private async Task<(bool, string)> VerifyHeaders(List<string> fileHeaders, int disciplineId,
            CancellationToken cancellationToken)
        {
            List<ResultUploadFieldMapping> acceptableHeaders =
                await GetHeadersToValidateWithFile(3, disciplineId, cancellationToken);
            if (fileHeaders.Count == 0)
            {
                return (false, "File headers cannot be empty or null.");
            }

            if (acceptableHeaders.Count == 0)
            {
                return (false, "No acceptable headers configuration found.");
            }

            var normalizedFileHeaders = fileHeaders
                .Select(h => h?.Trim()?.ToLowerInvariant() ?? string.Empty)
                .Where(h => !string.IsNullOrEmpty(h))
                .ToHashSet();

            var requiredHeaders = acceptableHeaders
                .Where(ah => !ah.IsOptional)
                .ToList();

            List<string> missingRequiredHeaders = [];
            foreach (var requiredHeader in requiredHeaders)
            {
                var normalizedRequired = requiredHeader.ColumnName.Trim().ToLowerInvariant();
                if (!normalizedFileHeaders.Contains(normalizedRequired))
                {
                    missingRequiredHeaders.Add(requiredHeader.ColumnName);
                }
            }

            if (!missingRequiredHeaders.Any()) return (true, string.Empty);
            {
                var errorMessage =
                    $"Missing required headers: {string.Join(", ", missingRequiredHeaders.Select(h => $"'{h}'"))}.";
                return (false, errorMessage);
            }
        }

        private async Task<List<ResultUploadFieldMapping>> GetHeadersToValidateWithFile(int scopeType, int scopeId,
            CancellationToken cancellationToken)
        {
            const string sqlQuery = """
                                    select f.ColumnName,f.IsOptional
                                    from validationscopes vsp
                                    inner join [ResultUploadFieldMapping] fm on vsp.ValidationScopeId = fm.ValidationScopeId
                                    inner join [ResultUploadFields] f on f.ResultUploadFieldId = fm.ResultUploadFieldId
                                    where vsp.ValidationScopeType = @ScopeType and vsp.ValidationScopeId = @ScopeId
                                    """;
            var queryParameters = new DynamicParameters();
            queryParameters.Add("ScopeType", scopeType);
            queryParameters.Add("ScopeId", scopeId);

            var repo = _readRepository.GetRepository<ResultUploadFieldMapping>();
            var item = (await repo.GetListAsync(sqlQuery, cancellationToken, queryParameters, null, QueryType.Text))
                .ToList();
            return item;
        }

        private static async Task<Result<string>> SendEmailToUser(IWriteRepositoryFactory writeRepositoryFactory,
            int fileId,
            CancellationToken cancellationToken)
        {
            try
            {
                var repo = writeRepositoryFactory.GetRepository<object>();

                var parameters = new DynamicParameters();
                parameters.Add("@FileId", fileId, DbType.Int32, ParameterDirection.Input);
                parameters.Add("@Success", dbType: DbType.Boolean,
                    direction: ParameterDirection.Output);
                parameters.Add("@ErrorMessage", dbType: DbType.String, size: int.MaxValue,
                    direction: ParameterDirection.Output);

                await repo.ExecuteAsync(
                    "SendFileUploadNotificationEmail",
                    cancellationToken,
                    parameters
                );

                var success = parameters.Get<bool>("@Success");
                var errorMessage = parameters.Get<string>("@ErrorMessage");

                return success
                    ? "Email notification sent successfully."
                    : Result<string>.Failure(errorMessage, ErrorType.InternalServerError);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(ex.Message, ErrorType.InternalServerError);
            }
        }

        private async Task ProcessMemberValidationAsync(ImportMemberCommand request, ResultUploadedFile uploadedFile,
            List<Dictionary<string, string>> fileData, IWriteRepositoryFactory writeRepositoryFactory,
            IReadRepositoryFactory readRepositoryFactory, DbTransaction transaction,
            IProgressTrackingService progressService, CancellationToken cancellationToken)
        {
            await SaveConfirmHeaderMapping(writeRepositoryFactory, request.FileDto, transaction, cancellationToken);
            uploadedFile.IsFinal = true;
            await UpdateResultUploadFile(writeRepositoryFactory, uploadedFile, transaction, cancellationToken);

            await progressService.SendProgressAsync(request.FileDto.WebSocketId, "Processing member data rows", 10,
                cancellationToken: cancellationToken);
            var dataTable = await ProcessRows(readRepositoryFactory, fileData, uploadedFile.DisciplineId,
                request.FileDto.GetMemberIdentifierColumn(), cancellationToken);

            await progressService.SendProgressAsync(request.FileDto.WebSocketId, "Validating members", 20,
                cancellationToken: cancellationToken);
            await SaveUploadedFileDataAsync(writeRepositoryFactory, dataTable, request.FileDto.FileId, transaction,
                cancellationToken);
        }
    }
}