using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Dapper;
using ExcelDataReader;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.Common.Enums;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands.Validators;
using JustGo.Result.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands;

internal static class ImportResultHelper
{
    internal const string GET_DISCIPLINE_SQL = """
                                               SELECT D.[Name], D.DisciplineId AS Id
                                               FROM ValidationScopes V
                                               INNER JOIN ResultDisciplines D on V.ScopeReferenceId = D.DisciplineId
                                               WHERE V.ValidationScopeType = 2 AND V.ValidationScopeId = @DisciplineId
                                               """;

    private const string BulkInsertMembersWithDataStoredProc = "sp_BulkInsertUploadedMembersWithData";
    private const int BatchSize = 2000;

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    internal static async Task HandlePreviousFileAsync(
        IWriteRepository<object> repo,
        int previousUploadFileId,
        int updatedBy,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var isExist = await VerifyUploadedFileExistsAsync(repo, previousUploadFileId, transaction, cancellationToken);

        if (!isExist)
            throw new CustomValidationException("Previous uploaded file does not exist or is already processed.");

        const string markPreviousAsDeletedSql = """
                                                UPDATE ResultUploadedFile
                                                SET IsDeleted = 1, Notes = CONCAT(Notes, ' | Marked as deleted by ', @UpdatedBy, ' on ', FORMAT(GETUTCDATE(), 'yyyy-MM-dd HH:mm:ss'))
                                                WHERE UploadedFileId = @UploadedFileId;

                                                UPDATE ResultUploadedMember
                                                SET IsDeleted = 1, Modified = 1
                                                WHERE UploadedFileId = @UploadedFileId;

                                                UPDATE ResultCompetition
                                                SET IsDeleted = 1
                                                WHERE UploadedFileId = @UploadedFileId;
                                                """;

        var parameters = new DynamicParameters();
        parameters.Add("UploadedFileId", previousUploadFileId);
        parameters.Add("UpdatedBy", updatedBy);

        await repo.ExecuteAsync(markPreviousAsDeletedSql, cancellationToken, parameters, transaction, QueryType.Text);
    }

    private static async Task<bool> VerifyUploadedFileExistsAsync(
        IWriteRepository<object> repo,
        int uploadedFileId,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string checkFileSql = """
                                    SELECT COUNT(1) 
                                    FROM ResultUploadedFile 
                                    WHERE UploadedFileId = @UploadedFileId 
                                        AND IsDeleted = 0 
                                        AND IsFinal = 0;
                                    """;

        var parameters = new DynamicParameters();
        parameters.Add("UploadedFileId", uploadedFileId);

        var count = await repo.ExecuteScalarAsync<int>(
            checkFileSql,
            cancellationToken,
            parameters,
            transaction,
            QueryType.Text);

        return count > 0;
    }

    internal static async Task BulkInsertUploadedMembersAsync(
        IWriteRepository<object> repo,
        int uploadedFileId,
        List<MemberWithDataRecord> memberWithDataRecords,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        foreach (var batch in memberWithDataRecords.Chunk(BatchSize))
        {
            for (int i = 0; i < batch.Length; i++)
            {
                batch[i].RowOrder = i;
            }

            var memberWithDataTable = CreateMemberWithDataTable(batch);

            await ExecuteBulkInsertMembersWithDataAsync(
                repo, uploadedFileId, memberWithDataTable, transaction, cancellationToken);
        }
    }

    private static DataTable CreateMemberWithDataTable(MemberWithDataRecord[] memberWithDataRecords)
    {
        var table = new DataTable();

        table.Columns.Add("UserId", typeof(int));
        table.Columns.Add("MemberId", typeof(string));
        table.Columns.Add("MemberName", typeof(string));
        table.Columns.Add("IsValidated", typeof(bool));
        table.Columns.Add("ErrorType", typeof(string));
        table.Columns.Add("ErrorMessage", typeof(string));
        table.Columns.Add("IsDeleted", typeof(bool));
        table.Columns.Add("Modified", typeof(bool));
        table.Columns.Add("MemberData", typeof(string)); // JSON data
        table.Columns.Add("RowOrder", typeof(int));

        foreach (var memberWithData in memberWithDataRecords)
        {
            table.Rows.Add(
                0,
                memberWithData.MemberId,
                null,
                0,
                memberWithData.ValidationResult == string.Empty ? "Validation Passed" : "Validation Failed",
                memberWithData.ValidationResult,
                false, // IsDeleted
                false, // Modified
                memberWithData.MemberData, // JSON serialized data
                memberWithData.RowOrder
            );
        }

        return table;
    }

    private static async Task ExecuteBulkInsertMembersWithDataAsync(
        IWriteRepository<object> repo,
        int uploadedFileId,
        DataTable memberWithDataTable,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            var parameters = new DynamicParameters();
            parameters.Add("@UploadedFileId", uploadedFileId, DbType.Int32);
            parameters.Add("@MemberDataRecords",
                memberWithDataTable.AsTableValuedParameter("dbo.UploadedMemberWithDataTableType"));

            await repo.ExecuteAsync(
                BulkInsertMembersWithDataStoredProc,
                cancellationToken,
                parameters,
                transaction,
                QueryType.StoredProcedure
            );
        }
        catch (Exception ex)
        {
            throw new CustomValidationException($"Error during bulk insert operation: {ex.Message}");
        }
    }

    public static async Task<int> CheckPreviousFileExistsAsync(IReadRepository<object> readRepository,
        int uploadFileId, DbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sqlQuery = """
                                SELECT TOP 1 PreviousUploadedFileId
                                FROM ResultUploadedFile
                                WHERE UploadedFileId = @UploadedFileId
                                """;

        var parameters = new DynamicParameters();
        parameters.Add("UploadedFileId", uploadFileId);

        var previousFileId = await readRepository.GetSingleAsync<int>(
            sqlQuery,
            parameters,
            transaction,
            cancellationToken,
            QueryType.Text);

        return previousFileId;
    }

    internal static async Task InsertSecondTabDataAsync(IWriteRepository<object> writeRepo, int fileDtoFileId,
        List<Dictionary<string, string>> secondTabData, DbTransaction transaction, CancellationToken cancellationToken)
    {
        if (secondTabData.Count == 0)
            return;

        var dataTable = new DataTable();
        dataTable.Columns.Add("UploadedFileId", typeof(int));
        dataTable.Columns.Add("Data", typeof(string));

        foreach (var row in secondTabData)
        {
            var officialDataJson = JsonSerializer.Serialize(row, JsonOptions);
            dataTable.Rows.Add(fileDtoFileId, officialDataJson);
        }

        const string bulkInsertQuery = """
                                       INSERT INTO ResultUploadedFileAdditionalData (UploadedFileId, [Data])
                                       SELECT UploadedFileId, [Data] 
                                       FROM @ResultAdditionalDataTableType
                                       """;

        var queryParameters = new DynamicParameters();
        queryParameters.Add("@ResultAdditionalDataTableType",
            dataTable.AsTableValuedParameter("ResultAdditionalDataTableType"));

        await writeRepo.ExecuteAsync(bulkInsertQuery, cancellationToken, queryParameters, transaction, QueryType.Text);
    }

    internal static async Task<int> GetCompulsoryFieldsCountAsync(IReadRepository<object> readRepository,
        int fileId, int sheetNumber, CancellationToken cancellationToken)
    {
        const string sqlQuery = """
                                select COUNT(1)
                                from ResultUploadedFile fa
                                inner join [ResultUploadFieldMapping] fm on fa.DisciplineId = fm.ValidationScopeId
                                inner join [ResultUploadFields] f on f.ResultUploadFieldId = fm.ResultUploadFieldId
                                where F.IsOptional = 0 AND fa.UploadedFileId = @FileId AND f.SheetNumber = @SheetNumber
                                """;

        var parameters = new DynamicParameters();
        parameters.Add("FileId", fileId);
        parameters.Add("SheetNumber", sheetNumber);

        var count = await readRepository.GetSingleAsync<int>(
            sqlQuery,
            parameters,
            null,
            cancellationToken,
            QueryType.Text);

        return count;
    }

    internal static async Task RunValidationAsync(IWriteRepository<object> repo, int uploadedFileId,
        int validationScopeId, CancellationToken cancellationToken)
    {
        await repo.ExecuteUnboundedAsync(
            "RuleEngineExecuteBulkValidation",
            cancellationToken,
            new { ValidationScopeId = validationScopeId, UploadedFileId = uploadedFileId });
    }

    internal static async Task DeleteUncommittedFileDataAsync(IWriteRepository<object> writeRepository,
        int fileId, CancellationToken cancellationToken)
    {
        const string deleteUncommittedFileDataQuery = """
                                                      DELETE md
                                                      FROM ResultUploadedMemberData AS md
                                                      INNER JOIN ResultUploadedMember AS m
                                                      	ON md.UploadedMemberId = m.UploadedMemberId
                                                      WHERE m.UploadedFileId = @UploadedFileId;

                                                      DELETE m
                                                      FROM ResultUploadedMember AS m
                                                      WHERE m.UploadedFileId = @UploadedFileId;
                                                      """;
        var queryParameters = new DynamicParameters();
        queryParameters.Add("@UploadedFileId", fileId);
        await writeRepository.ExecuteAsync(deleteUncommittedFileDataQuery, cancellationToken, queryParameters, null, QueryType.Text);
    }

    internal static async Task<string> GetResultUploadFieldValidationConfig(
        IReadRepositoryFactory readRepositoryFactory, int validationScopeId, int sheetNumber,
        CancellationToken cancellationToken)
    {
        const string sqlQuery = """
                                select top 1 Config
                                from [ResultUploadFieldValidationConfig]
                                where ValidationScopeId = @validationScopeId AND SheetNumber = @sheetNumber
                                """;

        var queryParameters = new DynamicParameters();
        queryParameters.Add("validationScopeId", validationScopeId);
        queryParameters.Add("sheetNumber", sheetNumber);

        var repo = readRepositoryFactory.GetRepository<string>();
        var item = await repo.GetSingleAsync<string>(sqlQuery, queryParameters, null,
            cancellationToken);
        return item ?? string.Empty;
    }
    
    internal static bool IsCancellationException(SqlException sqlException)
    {
        return sqlException.Number == 0 &&
               (sqlException.Message.Contains("Operation cancelled by user", StringComparison.OrdinalIgnoreCase));
    }
    
    internal static async Task UpdateOperationIdInUploadedFile(IWriteRepositoryFactory writeRepositoryFactory,
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
    
    internal static async Task UpdateFileStatus(IWriteRepositoryFactory writeRepositoryFactory, int fileId,
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
    
    internal static async Task<Result<bool>> ValidateRequiredFieldsMappedAsync(ImportResultFileCommand request,
        IReadRepositoryFactory readRepoFactory,
        CancellationToken cancellationToken)
    {
        var fieldsCount = await ImportResultHelper.GetCompulsoryFieldsCountAsync(
            readRepoFactory.GetRepository<object>(),
            request.FileDto.FileId, 1, cancellationToken);

        if (request.FileDto.ConfirmMemberHeaders.Count(x => x.IsMapped) < fieldsCount)
        {
            return Result<bool>.Failure(
                "Field mapping validation failed. Ensure all required fields are mapped correctly. Missing or incorrect mappings prevent further processing.",
                ErrorType.BadRequest);
        }

        var secondTabFieldsCount = await ImportResultHelper.GetCompulsoryFieldsCountAsync(
            readRepoFactory.GetRepository<object>(),
            request.FileDto.FileId, 2, cancellationToken);

        if (request.FileDto.ConfirmedSecondSheetHeaders.Count > 0 &&
            request.FileDto.ConfirmedSecondSheetHeaders.Count(x => x.IsMapped) != secondTabFieldsCount)
        {
            return Result<bool>.Failure(
                "Second sheet field mapping validation failed. Ensure all required fields are mapped correctly. Missing or incorrect mappings prevent further processing.",
                ErrorType.BadRequest);
        }

        return true;
    }
    
    internal static async Task<ResultUploadedFile> GetResultUploadedFileByFileId(int fileId,
        IReadRepositoryFactory readRepoFactory,
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
        var repo = readRepoFactory.GetRepository<ResultUploadedFile>();
        return (await repo.QueryFirstAsync<ResultUploadedFile>(getResultUploadedFileQuery, queryParameters, null,
            QueryType.Text, cancellationToken))!;
    }
    
    internal static void ParseXlsFile(
        Stream stream,
        List<ConfirmMemberDataDto> confirmMemberHeaders,
        List<Dictionary<string, string>> fileData,
        List<Dictionary<string, string>> secondTabData)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        using var reader = ExcelReaderFactory.CreateReader(stream);

        var headerMappingLookup = confirmMemberHeaders
            .Where(c => c.IsMapped)
            .ToDictionary(c => c.FileHeaderName, c => c.SystemColumnName, StringComparer.OrdinalIgnoreCase);

        int sheetIndex = 0;
        do
        {
            if (sheetIndex == 0)
            {
                ParseMainSheet(reader, headerMappingLookup, fileData);
            }
            else if (sheetIndex == 1)
            {
                ParseSecondSheet(reader, secondTabData);
                break;
            }

            sheetIndex++;
        } while (reader.NextResult());

        if (fileData.Count == 0)
        {
            throw new CustomValidationException(
                "The first sheet in the Excel file contains no data rows.  Please ensure the file is correctly formatted and includes data in the first sheet.");
        }
    }

    private static void ParseMainSheet(
        IExcelDataReader reader,
        Dictionary<string, string> headerMappingLookup,
        List<Dictionary<string, string>> fileData)
    {
        if (!reader.Read()) return;

        var headers = new string[reader.FieldCount];
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var headerValue = GetTrimmedString(reader.GetValue(i));
            if (string.IsNullOrEmpty(headerValue))
                headerValue = $"Column{i}";

            headers[i] = headerMappingLookup.TryGetValue(headerValue, out var mappedName)
                ? mappedName
                : headerValue;
        }

        while (reader.Read())
        {
            if (IsRowCompletelyEmpty(reader))
                continue;

            var rowDict = new Dictionary<string, string>(headers.Length);

            for (int i = 0; i < headers.Length && i < reader.FieldCount; i++)
            {
                var stringValue = GetTrimmedString(reader.GetValue(i));
                rowDict[headers[i]] = stringValue;
            }

            fileData.Add(rowDict);
        }
    }

    private static bool IsRowCompletelyEmpty(IExcelDataReader reader)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var value = reader.GetValue(i);
            if (value != null! && !string.IsNullOrWhiteSpace(value.ToString()))
            {
                return false;
            }
        }

        return true;
    }

    private static void ParseSecondSheet(
        IExcelDataReader reader,
        List<Dictionary<string, string>> secondTabData)
    {
        if (!reader.Read()) return;

        var headers = new string[reader.FieldCount];
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var headerValue = GetTrimmedString(reader.GetValue(i));
            if (string.IsNullOrEmpty(headerValue))
                headerValue = $"Column{i}";

            headers[i] = CapitalizeFirstLetter(headerValue);
        }

        while (reader.Read())
        {
            if (IsRowCompletelyEmpty(reader))
                continue;

            var rowDict = new Dictionary<string, string>(headers.Length);

            for (int i = 0; i < headers.Length && i < reader.FieldCount; i++)
            {
                var stringValue = GetTrimmedString(reader.GetValue(i));
                rowDict[headers[i]] = stringValue;
            }

            secondTabData.Add(rowDict);
        }
    }

    private static string GetTrimmedString(object? cellValue)
    {
        if (cellValue == null) return string.Empty;
        
        if (cellValue is DateTime dt)
            return dt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        
        var str = cellValue.ToString();
        if (string.IsNullOrEmpty(str)) return string.Empty;

        if (str.Length > 0 && (char.IsWhiteSpace(str[0]) || char.IsWhiteSpace(str[^1])))
        {
            return str.Trim();
        }

        return str;
    }

    private static string CapitalizeFirstLetter(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        if (input.Length == 1) return input.ToUpper();

        return char.ToUpper(input[0]) + input[1..];
    }
    
    internal static async Task<string?> GetMemberIdColumnNameByScopeId(IReadRepositoryFactory readRepositoryFactory,
        int validationScopeId,
        CancellationToken cancellationToken)
    {
        const string sqlQuery = """
                                select top 1 f.ColumnName
                                from [ResultUploadFieldMapping] fm
                                inner join [ResultUploadFields] f on f.ResultUploadFieldId = fm.ResultUploadFieldId
                                where fm.ValidationScopeId = @validationScopeId and f.ColumnIdentifier = 1
                                """;
        var queryParameters = new DynamicParameters();
        queryParameters.Add("validationScopeId", validationScopeId);
        var repo = readRepositoryFactory.GetRepository<ResultUploadFieldMapping>();
        var item = await repo.GetSingleAsync<string>(sqlQuery, queryParameters, null,
            cancellationToken);
        return item;
    }
    
    internal static List<MemberWithDataRecord> ProcessFileDataWithOrder(List<Dictionary<string, string>> fileData,
        string? memberIdHeader, IFileDataValidator? validator)
    {
        var memberWithDataRecords = new List<MemberWithDataRecord>();
        bool runValidation = validator != null;

        for (int index = 0; index < fileData.Count; index++)
        {
            var row = fileData[index];

            var memberId = string.Empty;
            if (!string.IsNullOrEmpty(memberIdHeader))
            {
                memberId = row.GetValueOrDefault(memberIdHeader, string.Empty);
            }

            var additionalData = row
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var validationResult = string.Empty;
            if (runValidation)
            {
                var result = validator!.ValidateRow(additionalData);
                validationResult = string.Join(", ", result.Select(e => e.ToString()));
            }

            var memberDataJson = JsonSerializer.Serialize(additionalData, ImportResultHelper.JsonOptions);

            memberWithDataRecords.Add(new MemberWithDataRecord
            {
                MemberId = memberId,
                MemberData = memberDataJson,
                RowOrder = index,
                ValidationResult = validationResult
            });
        }

        return memberWithDataRecords;
    }
}

internal class MemberWithDataRecord
{
    public string MemberId { get; set; } = string.Empty;
    public string MemberData { get; set; } = string.Empty;
    public int RowOrder { get; set; }
    public string ValidationResult { get; set; } = string.Empty;
}