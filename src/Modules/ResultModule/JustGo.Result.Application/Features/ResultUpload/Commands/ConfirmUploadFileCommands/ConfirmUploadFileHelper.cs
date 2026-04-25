using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.Common.Enums;
using JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.Infos;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands;
using Microsoft.Data.SqlClient;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands;

public static class ConfirmUploadFileHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
    
    private const int DefaultBatchSize = 1000;
    private const int ResultDataBatchSize = 500;

    internal static async Task ValidateFileEligibilityAsync(int uploadedFileId,
        IUtilityService utilityService,
        IReadRepository<object> readRepository,
        IWriteRepository<object> writeRepository,
        DbTransaction coordinatorTransaction,
        CancellationToken cancellationToken)
    {
        await CheckFileAlreadyConfirmed(uploadedFileId, readRepository, coordinatorTransaction,
            cancellationToken);

        await CanConfirmFileUploadAsync(uploadedFileId, readRepository, coordinatorTransaction,
            cancellationToken);

        await HandlePreviousFileAsync(readRepository, utilityService, writeRepository, uploadedFileId,
            coordinatorTransaction, cancellationToken);
    }

    private static async Task CheckFileAlreadyConfirmed(int uploadFileId, IReadRepository<object> readRepo,
        IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string checkSql = """
                                SELECT COUNT(1)
                                FROM ResultCompetition C
                                WHERE C.UploadedFileId = @UploadedFileId AND C.IsDeleted = 0
                                """;

        var existingCount = await readRepo.GetSingleAsync<int>(
            checkSql, new { UploadedFileId = uploadFileId }, transaction, cancellationToken);

        if (existingCount > 0)
        {
            throw new CustomValidationException("The uploaded file has already been confirmed.");
        }
    }

    private static async Task CanConfirmFileUploadAsync(int requestUploadFileId, IReadRepository<object> readRepository,
        DbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT COUNT(1) AS ErrorCount
                           FROM ResultUploadedFile UF
                           INNER JOIN ResultUploadedMember UM ON UF.UploadedFileId = UM.UploadedFileId
                           WHERE UF.UploadedFileId = @UploadedFileId
                               AND UF.IsDeleted = 0
                               AND UM.IsDeleted = 0
                               AND UM.ErrorMessage <> ''
                           """;

        var parameters = new DynamicParameters();
        parameters.Add("UploadedFileId", requestUploadFileId);

        var count = await readRepository.GetSingleAsync<int>(sql, parameters, transaction, cancellationToken,
            QueryType.Text);

        if (count > 0)
        {
            throw new CustomValidationException(
                "The uploaded file cannot be confirmed because it may have already been processed or contains validation errors. Please review the file and try again.");
        }
    }

    private static async Task HandlePreviousFileAsync(IReadRepository<object> readRepository,
        IUtilityService utilityService,
        IWriteRepository<object> writeRepository, int uploadFileId,
        DbTransaction transaction, CancellationToken cancellationToken)
    {
        var previousFileIdExists = await ImportResultHelper.CheckPreviousFileExistsAsync(readRepository,
            uploadFileId, transaction, cancellationToken);

        if (previousFileIdExists > 0)
        {
            await ImportResultHelper.HandlePreviousFileAsync(writeRepository, previousFileIdExists,
                await utilityService.GetCurrentUserId(cancellationToken), transaction, cancellationToken);
        }
    }

    internal static async Task<FileMetadata> GetFileMetadataAsync(int uploadFileId,
        IReadRepository<object> readRepository,
        DbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT TOP 1
                               UF.EventId,
                               UF.DisciplineId,
                               UF.UploadedFileId,
                               UF.[FileName]
                           FROM ResultUploadedFile UF
                           INNER JOIN ResultUploadedMember UM ON UF.UploadedFileId = UM.UploadedFileId
                           WHERE UF.UploadedFileId = @UploadedFileId
                               AND UF.IsDeleted = 0
                               AND UM.IsDeleted = 0
                           """;

        var fileInfo = await readRepository.QueryFirstAsync<UploadFileInfo>(sql,
            new { UploadedFileId = uploadFileId }, transaction, QueryType.Text, cancellationToken);

        if (fileInfo == null)
        {
            throw new CustomValidationException(
                "No data found for the provided upload file. Please verify the file ID and ensure the file contains valid data.");
        }

        var disciplineId = fileInfo.DisciplineId;

        var discipline = await GetDisciplineAsync(readRepository, disciplineId, transaction, cancellationToken);

        return new FileMetadata
        {
            Discipline = discipline,
            EventId = fileInfo.EventId,
            UploadedFileId = fileInfo.UploadedFileId,
            FileName = fileInfo.FileName
        };
    }

    private static async Task<(string Name, int Id, string? ClassKey)> GetDisciplineAsync(
        IReadRepository<object> readRepository,
        int disciplineId, DbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT D.[Name], D.DisciplineId AS Id, S.ClassKey
                           FROM ValidationScopes S
                           INNER JOIN ResultDisciplines D ON S.ScopeReferenceId = D.DisciplineId
                           WHERE ValidationScopeType = 2 AND ValidationScopeId = @DisciplineId
                           """;

        var discipline = await readRepository.QueryFirstAsync<(string Name, int Id, string? ClassKey)>(sql,
            new { DisciplineId = disciplineId }, transaction, QueryType.Text, cancellationToken);

        if (string.IsNullOrEmpty(discipline.Name))
        {
            throw new CustomValidationException($"Discipline not found with ID {disciplineId}");
        }

        return discipline;
    }

    internal static async Task<List<(string SourceFieldName, string DestinationFieldName)>>
        GetDisciplineFieldMappingsAsync(
            int disciplineId, string tableName, IReadRepositoryFactory readRepository,
            CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            tableName = "ResultCompetitionResultData";
        }

        const string getDisciplineMappingsSql = """
                                                SELECT SourceFieldName, DestinationFieldName
                                                FROM ResultFieldMapping
                                                WHERE DisciplineId = @DisciplineId
                                                    AND TableName = @TableName
                                                    AND IsActive = 1
                                                ORDER BY SourceFieldName
                                                """;

        var parameters = new DynamicParameters();
        parameters.Add("DisciplineId", disciplineId);
        parameters.Add("TableName", tableName.Trim());

        var mappings = await readRepository.GetRepository<object>()
            .GetListAsync<(string SourceFieldName, string DestinationFieldName)>(
                getDisciplineMappingsSql, parameters, null, QueryType.Text, cancellationToken);

        return mappings.ToList();
    }

    internal static async Task<List<ClassInfo>> GetUniqueClassInfoFromDatabaseAsync(
        IReadRepositoryFactory readRepository,
        int uploadedFileId,
        (string Name, int Id, string? ClassKey) discipline,
        List<string> competitionAdditionalFields,
        CancellationToken cancellationToken)
    {
        var additionalFieldsSelection = competitionAdditionalFields.Count > 0
            ? "," + string.Join(",\n                ", competitionAdditionalFields.Select(field =>
                $"MIN(JSON_VALUE(MD.MemberData, '$.\"{EscapeJsonPath(field)}\"')) as [{field}]"))
            : string.Empty;

        var selectAllOrTop1 = !string.IsNullOrWhiteSpace(discipline.ClassKey) ? "" : "TOP 1";

        var classKeySelection = !string.IsNullOrWhiteSpace(discipline.ClassKey)
            ? $"JSON_VALUE(MD.MemberData, '$.\"{EscapeJsonPath(discipline.ClassKey)}\"') as ClassName,"
            : string.Empty;

        var classKeyWhereClause = !string.IsNullOrWhiteSpace(discipline.ClassKey)
            ? $"AND JSON_VALUE(MD.MemberData, '$.\"{EscapeJsonPath(discipline.ClassKey)}\"') IS NOT NULL\n           AND JSON_VALUE(MD.MemberData, '$.\"{EscapeJsonPath(discipline.ClassKey)}\"') != ''"
            : string.Empty;

        var classKeyGroupBy = !string.IsNullOrWhiteSpace(discipline.ClassKey)
            ? $"GROUP BY JSON_VALUE(MD.MemberData, '$.\"{EscapeJsonPath(discipline.ClassKey)}\"')"
            : string.Empty;


        var sql = $"""
                   SELECT
                       {selectAllOrTop1} {classKeySelection}
                       MIN(JSON_VALUE(MD.MemberData, '$."CompStartDate"')) as CompStartDate,
                       MIN(JSON_VALUE(MD.MemberData, '$."CompEndDate"')) as CompEndDate
                       {additionalFieldsSelection}
                   FROM ResultUploadedFile UF
                   INNER JOIN ResultUploadedMember UM ON UF.UploadedFileId = UM.UploadedFileId
                   INNER JOIN ResultUploadedMemberData MD ON UM.UploadedMemberId = MD.UploadedMemberId
                   WHERE UF.UploadedFileId = @UploadedFileId
                       AND UF.IsDeleted = 0
                       AND UM.IsDeleted = 0
                       {classKeyWhereClause}
                   {classKeyGroupBy}
                   """;

        var parameters = new DynamicParameters();
        parameters.Add("UploadedFileId", uploadedFileId);

        var repo = readRepository.GetRepository<object>();
        var results = await repo.GetListAsync<dynamic>(
            sql, parameters, null, QueryType.Text, cancellationToken);

        List<ClassInfo> classInfos = [];
        foreach (var row in results)
        {
            var classInfo = CreateClassInfoFromDynamic(discipline, row, competitionAdditionalFields) as ClassInfo;
            classInfos.Add(classInfo!);
        }

        return classInfos;
    }

    private static string EscapeJsonPath(string jsonPath)
    {
        return jsonPath.Replace("'", "''").Replace("\"", "\\\"");
    }


    private static ClassInfo CreateClassInfoFromDynamic((string Name, int Id, string? ClassKey) discipline, dynamic row,
        List<string> competitionAdditionalFields)
    {
        var rowDict = (IDictionary<string, object>)row;

        var additionalData = new Dictionary<string, string>();
        foreach (var field in competitionAdditionalFields)
        {
            if (!rowDict.TryGetValue(field, out var value) || value == null!) continue;

            var stringValue = value.ToString()?.Trim();
            if (!string.IsNullOrEmpty(stringValue))
            {
                additionalData[field] = stringValue;
            }
        }

        var currentDate = DateTime.UtcNow.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        rowDict.TryGetValue("ClassName", out var className);

        className ??= discipline.Name;
        
        rowDict.TryGetValue("CompStartDate", out var startDateObj);
        var startDateString = startDateObj?.ToString()?.Trim();

        rowDict.TryGetValue("CompEndDate", out var endDateObj);
        var endDateString = endDateObj?.ToString()?.Trim();

        if (string.IsNullOrWhiteSpace(startDateString))
        {
            startDateString = currentDate;
        }

        if (string.IsNullOrWhiteSpace(endDateString))
        {
            endDateString = currentDate;
        }

        return new ClassInfo
        {
            ClassName = className.ToString(),
            CompStartDate = startDateString ?? currentDate,
            CompEndDate = endDateString ?? currentDate,
            AdditionalData = additionalData.Count > 0
                ? JsonSerializer.Serialize(additionalData, JsonOptions)
                : string.Empty
        };

    }

    internal static async Task<int> InsertResultCompetitionAsync(CompetitionInfo competitionInfo,
        RepositoryContext repositoryContext, CancellationToken cancellationToken)
    {
        // Resolve or create class
        var classId = 0;
        if (competitionInfo.ClassName is not null)
        {
            const string resolveClassIdSql = "SELECT ClassId FROM ResultClass WHERE ClassName = @ClassName";
            classId = await repositoryContext.ReadRepo.GetSingleAsync<int?>(
                resolveClassIdSql, new { ClassName = competitionInfo.ClassName },
                repositoryContext.Transaction, cancellationToken) ?? 0;

            if (classId is 0)
            {
                const string insertClassSql = """
                                              INSERT INTO ResultClass (ClassName)
                                              VALUES (@ClassName);
                                              SELECT CAST(SCOPE_IDENTITY() AS INT);
                                              """;
                classId = await repositoryContext.WriteRepo.ExecuteScalarAsync<int>(
                    insertClassSql, cancellationToken, new { ClassName = competitionInfo.ClassName },
                    repositoryContext.Transaction, QueryType.Text);
            }
        }

        // Insert competition
        const string insertResultCompetitionSql = """
                                                  INSERT INTO ResultCompetition (EventId, CompetitionName, DisciplineId, ClassId, CompetitionStatusId, UploadedFileId, StartDate, EndDate, AdditionalData)
                                                  VALUES (@EventId, @CompetitionName, @DisciplineId, @ClassId, @CompetitionStatusId, @UploadedFileId, @StartDate, @EndDate, @AdditionalData);
                                                  SELECT CAST(SCOPE_IDENTITY() AS INT);
                                                  """;

        var insertResultCompetitionParams = new DynamicParameters();
        insertResultCompetitionParams.Add("EventId", competitionInfo.EventId);
        insertResultCompetitionParams.Add("CompetitionName", competitionInfo.ClassName);
        insertResultCompetitionParams.Add("DisciplineId", competitionInfo.DisciplineId);
        insertResultCompetitionParams.Add("ClassId", classId);
        insertResultCompetitionParams.Add("CompetitionStatusId", (int)ResultCompetitionStatus.Draft);
        insertResultCompetitionParams.Add("UploadedFileId", competitionInfo.UploadedFileId);
        insertResultCompetitionParams.Add("StartDate", competitionInfo.StartDate.Date);
        insertResultCompetitionParams.Add("EndDate", competitionInfo.EndDate.Date);
        insertResultCompetitionParams.Add("AdditionalData", competitionInfo.AdditionalData);

        var competitionId = await repositoryContext.WriteRepo.ExecuteScalarAsync<int>(
            insertResultCompetitionSql, cancellationToken, insertResultCompetitionParams,
            repositoryContext.Transaction, QueryType.Text);

        return competitionId <= 0
            ? throw new CustomValidationException("Failed to insert ResultCompetition. Please check the provided data.")
            : competitionId;
    }

    internal static async Task<int> InsertResultCompetitionRound(RoundInfo roundInfo,
        int competitionId, RepositoryContext repositoryContext, CancellationToken cancellationToken)
    {
        const string resolveCompetitionCategoryIdSql = """
                                                       SELECT CompetitionCategoryId
                                                       FROM ResultCompetitionCategory
                                                       WHERE CompetitionCategoryName = @CompetitionCategoryName
                                                       """;
        var competitionCategoryId = await repositoryContext.ReadRepo.GetSingleAsync<int?>(
            resolveCompetitionCategoryIdSql, new { CompetitionCategoryName = roundInfo.RoundName },
            repositoryContext.Transaction, cancellationToken) ?? 0;

        const string insertResultCompetitionRoundSql = """
                                                       INSERT INTO ResultCompetitionRounds (CompetitionId, RoundName, CompetitionCategoryId, StartDate, EndDate)
                                                       VALUES (@CompetitionId, @RoundName, @CompetitionCategoryId, @StartDate, @EndDate);
                                                       SELECT CAST(SCOPE_IDENTITY() AS INT);
                                                       """;

        var insertResultCompetitionRoundParams = new DynamicParameters();
        insertResultCompetitionRoundParams.Add("CompetitionId", competitionId);
        insertResultCompetitionRoundParams.Add("RoundName", roundInfo.RoundName);
        insertResultCompetitionRoundParams.Add("CompetitionCategoryId", competitionCategoryId);
        insertResultCompetitionRoundParams.Add("StartDate", roundInfo.RoundStartDate);
        insertResultCompetitionRoundParams.Add("EndDate", roundInfo.RoundEndDate);

        var competitionRoundId = await repositoryContext.WriteRepo.ExecuteScalarAsync<int>(
            insertResultCompetitionRoundSql, cancellationToken, insertResultCompetitionRoundParams,
            repositoryContext.Transaction, QueryType.Text);

        if (competitionRoundId <= 0)
        {
            throw new CustomValidationException(
                "Failed to insert ResultCompetitionRound. Please check the provided data.");
        }

        return competitionRoundId;
    }

    internal static async Task<List<ParticipantBatchData>> GetParticipantBatchForClassAsync(
        int uploadedFileId,
        string? className,
        string? classKey,
        int offset,
        int batchSize,
        IReadRepository<object> readRepository,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var classKeyWhereClause = !string.IsNullOrWhiteSpace(classKey)
            ? "AND JSON_VALUE(MD.MemberData, CONCAT('$.\"', REPLACE(@ClassKey, '\"', '\\\"'), '\"')) = @ClassName"
            : string.Empty;

        string sql = $"""
                      SELECT
                          UM.UserId,
                          UM.UploadedMemberId,
                          MD.MemberData
                      FROM ResultUploadedFile UF
                      INNER JOIN ResultUploadedMember UM ON UF.UploadedFileId = UM.UploadedFileId
                      INNER JOIN ResultUploadedMemberData MD ON UM.UploadedMemberId = MD.UploadedMemberId
                      WHERE UF.UploadedFileId = @UploadedFileId
                          AND UF.IsDeleted = 0
                          AND UM.IsDeleted = 0
                          {classKeyWhereClause}
                      ORDER BY UM.UserId
                      OFFSET @Offset ROWS
                      FETCH NEXT @BatchSize ROWS ONLY
                      """;

        var parameters = new DynamicParameters();
        parameters.Add("UploadedFileId", uploadedFileId, DbType.Int32);
        parameters.Add("ClassKey", classKey, DbType.String, size: 255);
        parameters.Add("ClassName", className, DbType.String, size: 255);
        parameters.Add("Offset", offset, DbType.Int32);
        parameters.Add("BatchSize", batchSize, DbType.Int32);

        try
        {
            var rawResults = await readRepository.GetListAsync<ParticipantRawDataDto>(
                sql, parameters, transaction, QueryType.Text, cancellationToken);

            return rawResults.Select(dto => new ParticipantBatchData
                {
                    UserId = dto.UserId,
                    UploadedMemberId = dto.UploadedMemberId,
                    MemberDataJson = dto.MemberData ?? string.Empty
                })
                .Where(batch => !string.IsNullOrEmpty(batch.MemberDataJson))
                .Select(batch =>
                {
                    batch.ParseMemberData();
                    return batch;
                })
                .ToList();
        }
        catch (SqlException sqlEx)
        {
            throw new CustomValidationException(
                $"Database error while retrieving participants for class '{className}': {sqlEx.Message}");
        }
        catch (JsonException jsonEx)
        {
            throw new CustomValidationException(
                $"Invalid JSON data found in member data for class '{className}': {jsonEx.Message}");
        }
        catch (Exception ex)
        {
            throw new CustomValidationException(
                $"Unexpected error while processing participants for class '{className}': {ex.Message}");
        }
    }
    
    internal static async Task<List<(int CompetitionParticipantId, int UserId)>> InsertParticipantBatchAsync(
        int[] userIds, int competitionRoundId, IWriteRepository<object> writeRepo,
        DbTransaction transaction, CancellationToken cancellationToken)
    {
        if (userIds.Length == 0) return [];

        var valuesClauses = userIds.Select((_, index) => $"(@CompetitionRoundId, @UserId{index})");
        var bulkInsertSql = $"""
                             INSERT INTO ResultCompetitionParticipants (CompetitionRoundId, UserId)
                             OUTPUT INSERTED.CompetitionParticipantId, INSERTED.UserId
                             VALUES {string.Join(", ", valuesClauses)}
                             """;

        var insertParams = new DynamicParameters();
        insertParams.Add("CompetitionRoundId", competitionRoundId, DbType.Int32);

        for (var i = 0; i < userIds.Length; i++)
        {
            insertParams.Add($"UserId{i}", userIds[i], DbType.Int32);
        }

        var results = await writeRepo.ExecuteQueryAsync<(int CompetitionParticipantId, int UserId)>(
            bulkInsertSql, cancellationToken, insertParams, transaction, QueryType.Text);

        return results.ToList();
    }
    
    internal static async Task<List<(int UserId, int CompetitionResultId, int CompetitionParticipantId)>>
        InsertResultsBatchAsync(
            IReadRepositoryFactory readRepository,
            List<ParticipantBatchData> participantBatch,
            List<(int CompetitionParticipantId, int UserId)> participantIds,
            int disciplineId,
            IWriteRepository<object> writeRepository,
            DbTransaction transaction,
            CancellationToken cancellationToken)
    {
        var resultFields = await GetDisciplineFieldMappingsAsync(
            disciplineId, "ResultCompetitionResults", readRepository, cancellationToken);

        if (participantIds.Count == 0) return [];

        var destinationFields = string.Join(", ", resultFields.Select(f => $"[{f.DestinationFieldName}]"));

        var insertParams = new DynamicParameters();
        insertParams.Add("ResultType", 1);

        var valuesClauses = new List<string>();

        for (var i = 0; i < participantIds.Count; i++)
        {
            var (competitionParticipantId, userId) = participantIds[i];

            var participantData = participantBatch.FirstOrDefault(p => p.UserId == userId && !p.IsProcessed);

            if (participantData == null)
            {
                throw new CustomValidationException(
                    $"No matching participant data found for UserId {userId}");
            }

            participantData.MarkAsProcessed();

            insertParams.Add($"CompetitionParticipantId{i}", competitionParticipantId, DbType.Int32);

            var fieldPlaceholders = new List<string> { $"@CompetitionParticipantId{i}", "@ResultType" };

            foreach (var field in resultFields)
            {
                var fieldValue = participantData.ParsedMemberData.TryGetValue(field.SourceFieldName, out var value)
                    ? value?.Trim()
                    : null;

                var paramName = $"{field.DestinationFieldName}{i}";

                insertParams.Add(paramName, string.IsNullOrWhiteSpace(fieldValue) ? null : fieldValue, DbType.String);

                fieldPlaceholders.Add($"@{paramName}");
            }

            valuesClauses.Add($"({string.Join(", ", fieldPlaceholders)})");
        }

        var bulkInsertSql = $"""
                             INSERT INTO ResultCompetitionResults (CompetitionParticipantId, ResultType, {destinationFields})
                             OUTPUT INSERTED.CompetitionResultId, INSERTED.CompetitionParticipantId
                             VALUES {string.Join(", ", valuesClauses)}
                             """;

        try
        {
            var insertedResults =
                await writeRepository.ExecuteQueryAsync<(int CompetitionResultId, int CompetitionParticipantId)>(
                    bulkInsertSql, cancellationToken, insertParams, transaction, QueryType.Text);

            var resultList = insertedResults.ToList();
            var userIdToResultIdList = new List<(int UserId, int CompetitionResultId, int CompetitionParticipantId)>();

            foreach (var result in resultList)
            {
                var participant = participantIds.FirstOrDefault(p =>
                    p.CompetitionParticipantId == result.CompetitionParticipantId);

                if (participant != default)
                {
                    userIdToResultIdList.Add((participant.UserId, result.CompetitionResultId,
                        result.CompetitionParticipantId));
                }
            }

            return userIdToResultIdList;
        }
        catch (SqlException sqlEx)
        {
            throw new CustomValidationException(
                $"Database error while inserting competition results: {sqlEx.Message}");
        }
        catch (Exception ex)
        {
            throw new CustomValidationException(
                $"Failed to insert competition results: {ex.Message}");
        }
    }
    
    internal static async Task InsertResultDataBatchAsync(IReadRepositoryFactory readRepository,
        List<ParticipantBatchData> participantBatch,
        List<(int UserId, int CompetitionResultId, int CompetitionParticipantId)> resultIds,
        int disciplineId,
        IWriteRepository<object> writeRepository,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var resultFields = (await GetDisciplineFieldMappingsAsync(
                disciplineId, "ResultCompetitionResultData", readRepository, cancellationToken))
            .Select(x => x.SourceFieldName).ToList();

        if (resultFields.Count == 0) return;

        var insertDataList = new List<(int CompetitionResultId, string Key, string Value, int DataType)>();

        foreach (var (userId, competitionResultId, competitionParticipantId) in resultIds)
        {
            var userBatch = participantBatch
                .FirstOrDefault(p =>
                    p.UserId == userId && p.ParticipantId == competitionParticipantId && !p.IsProcessed);

            if (userBatch?.ParsedMemberData == null) continue;
            userBatch.MarkAsProcessed();

            foreach (var (key, value) in userBatch.ParsedMemberData)
            {
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value)) continue;
                if (!resultFields.Contains(key, StringComparer.OrdinalIgnoreCase)) continue;

                var val = value.Trim();
                var dType = DetermineDataType(val);
                insertDataList.Add((competitionResultId, key, val, dType));
            }
        }

        if (insertDataList.Count == 0) return;

        foreach (var batch in insertDataList.Chunk(ResultDataBatchSize))
        {
            await ProcessResultDataBatch(batch.ToList(), writeRepository, transaction, cancellationToken);
        }
    }

    internal static async Task ProcessResultDataBatch(
        List<(int CompetitionResultId, string Key, string Value, int DataType)> batchData,
        IWriteRepository<object> writeRepo,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var valuesClauses = batchData.Select((_, index) =>
            $"(@CompetitionResultId{index}, @Key{index}, @Value{index}, @DataType{index})");

        var bulkInsertSql = $"""
                             INSERT INTO ResultCompetitionResultData (CompetitionResultId, [Key], [Value], DataType)
                             VALUES {string.Join(", ", valuesClauses)}
                             """;

        var insertParams = new DynamicParameters();
        for (var i = 0; i < batchData.Count; i++)
        {
            var (competitionResultId, key, value, dataType) = batchData[i];
            insertParams.Add($"CompetitionResultId{i}", competitionResultId);
            insertParams.Add($"Key{i}", key);
            insertParams.Add($"Value{i}", value);
            insertParams.Add($"DataType{i}", dataType);
        }

        await writeRepo.ExecuteAsync(bulkInsertSql, cancellationToken, insertParams, transaction, QueryType.Text);
    }

    internal static int DetermineDataType(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 5;
        if (int.TryParse(value, out _)) return 0;
        if (decimal.TryParse(value, out _)) return 1;
        if (DateTime.TryParse(value, out _)) return 3;
        if (value.Trim().StartsWith('{') && value.Trim().EndsWith('}')) return 7;
        if (value.Trim().StartsWith('[') && value.Trim().EndsWith(']')) return 8;
        return 5; // Default to text
    }
}

public class UploadFileInfo
{
    public int EventId { get; set; }
    public int DisciplineId { get; set; }
    public int UploadedFileId { get; set; }
    public string FileName { get; set; } = string.Empty;
}

public class FileMetadata
{
    public (string Name, int Id, string? ClassKey) Discipline { get; init; }
    public int EventId { get; init; }
    public int UploadedFileId { get; init; }
    public string FileName { get; set; } = string.Empty;
}

public class ParticipantBatchData
{
    public int UserId { get; init; }
    public int UploadedMemberId { get; set; }
    public string MemberDataJson { get; init; } = string.Empty;
    public Dictionary<string, string?> ParsedMemberData { get; private set; } = new();
    private bool IsJsonParsed { get; set; }
    public bool IsProcessed { get; private set; }
    public int ParticipantId { get; set; }

    public void ParseMemberData()
    {
        if (IsJsonParsed || string.IsNullOrEmpty(MemberDataJson)) return;

        try
        {
            ParsedMemberData = JsonSerializer.Deserialize<Dictionary<string, string?>>(MemberDataJson)
                               ?? new Dictionary<string, string?>();
            IsJsonParsed = true;
        }
        catch
        {
            ParsedMemberData = new Dictionary<string, string?>();
            IsJsonParsed = true;
        }
    }

    public string? GetHorseId()
    {
        if (!IsJsonParsed) ParseMemberData();

        return ParsedMemberData.TryGetValue("Horse ID", out var horseId)
            ? horseId?.Trim()
            : null;
    }

    public void MarkAsProcessed()
    {
        IsProcessed = true;
    }

    public void ResetProcessingMarker()
    {
        IsProcessed = false;
    }
}

public class ParticipantRawDataDto
{
    public int UserId { get; set; }
    public int UploadedMemberId { get; set; }
    public string MemberData { get; set; } = string.Empty;
}