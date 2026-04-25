using System.Data;
using System.Data.Common;
using System.Text.Json;
using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.UploadResultDtos;
using JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.Infos;
using ResultCompetitionStatus = JustGo.Result.Application.Common.Enums.ResultCompetitionStatus;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.ConfirmUploadEquestrian;

public class EventingDisciplineHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IWriteRepositoryFactory _writeRepository;
    private readonly IReadRepositoryFactory _readRepository;
    private readonly IUnitOfWork _unitOfWork;

    private const string UploadResultFileSqlQuery = """
                                                    SELECT
                                                        UF.EventId,
                                                        UF.DisciplineId,
                                                        UF.UploadedFileId,
                                                        UF.[FileName],
                                                        UM.MemberId,
                                                        MD.MemberData,
                                                        UM.UserId
                                                    FROM ResultUploadedFile UF
                                                    INNER JOIN ResultUploadedMember UM ON UF.UploadedFileId = UM.UploadedFileId
                                                    INNER JOIN ResultUploadedMemberData MD ON UM.UploadedMemberId = MD.UploadedMemberId
                                                    WHERE UF.UploadedFileId = @UploadedFileId
                                                        AND UF.IsDeleted = 0
                                                        AND UM.IsDeleted = 0
                                                    """;

    private const string InsertResultCompetitionSql = """
                                                      INSERT INTO ResultCompetition (EventId, CompetitionName, DisciplineId, ClassId, CompetitionStatusId, UploadedFileId, StartDate, EndDate, AdditionalData)
                                                      VALUES (@EventId, @CompetitionName, @DisciplineId, @ClassId, @CompetitionStatusId, @UploadedFileId, @StartDate, @EndDate, @AdditionalData);
                                                      SELECT CAST(SCOPE_IDENTITY() AS INT);
                                                      """;

    private const string GetDisciplineSql = """
                                            SELECT D.[Name], D.DisciplineId AS Id
                                            FROM ValidationScopes S
                                            INNER JOIN ResultDisciplines D ON S.ScopeReferenceId = D.DisciplineId
                                            WHERE ValidationScopeType = 2 AND ValidationScopeId = @DisciplineId
                                            """;

    public EventingDisciplineHandler(IWriteRepositoryFactory writeRepository,
        IReadRepositoryFactory readRepository, IUnitOfWork unitOfWork)
    {
        _writeRepository = writeRepository;
        _readRepository = readRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(ConfirmUploadFileCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var repo = _readRepository.GetRepository<UploadedFileDetailDto>();
            var readRepository = _readRepository.GetRepository<object>();
            var writeRepository = _writeRepository.GetRepository<object>();

            var uploadedFileDetail =
                await GetUploadedResultFileDataAsync(request.UploadFileId, repo, transaction,
                    cancellationToken);

            var fileData =
                await ValidateAndExtractFileDataAsync(uploadedFileDetail, readRepository, transaction,
                    cancellationToken);

            return await ProcessEventingCompetitiveDisciplineAsync(fileData.EventId, fileData.UploadedFileId,
                fileData.Discipline.Id,
                readRepository, writeRepository, transaction, uploadedFileDetail, cancellationToken);
        }
        catch (CustomValidationException exception)
        {
            await transaction.RollbackAsync();

            // CustomLog.Audit(AuditScheme.ResultManagement.Value,
            //     AuditScheme.ResultManagement.ResultUpload.Value,
            //     AuditScheme.ResultManagement.ResultUpload.ExceptionOccurred.Value,
            //     0,
            //     0,
            //     EntityType.Result,
            //     0,
            //     nameof(AuditLogSink.ActionType.Created),
            //     exception.Message
            // );

            return Result<int>.Failure(exception.Message, ErrorType.BadRequest);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            // CustomLog.Audit(AuditScheme.ResultManagement.Value,
            //     AuditScheme.ResultManagement.ResultUpload.Value,
            //     AuditScheme.ResultManagement.ResultUpload.ExceptionOccurred.Value,
            //     0,
            //     0,
            //     EntityType.Result,
            //     0,
            //     nameof(AuditLogSink.ActionType.Created),
            //     ex.Message
            // );

            return Result<int>.Failure(
                "An unexpected error occurred while processing the upload file. Please try again later or contact support if the issue persists.",
                ErrorType.InternalServerError);
        }
    }

    private static async Task<FileProcessingContext> ValidateAndExtractFileDataAsync(
        UploadedFileDetailDto[] uploadedFileDetail,
        IReadRepository<object> readRepository,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var disciplineId = uploadedFileDetail.FirstOrDefault(x => x.DisciplineId.HasValue)?.DisciplineId;
        if (!disciplineId.HasValue)
        {
            throw new CustomValidationException(
                "Discipline information is missing or invalid in the uploaded file. Please ensure the file contains valid discipline data and try again.");
        }

        var discipline = await GetDisciplineAsync(readRepository, disciplineId.Value, transaction, cancellationToken);

        var eventId = uploadedFileDetail.FirstOrDefault(x => x.EventId.HasValue)?.EventId;
        if (!eventId.HasValue)
        {
            throw new CustomValidationException("Event information is required but not found for the uploaded file.");
        }

        var uploadedFileId = uploadedFileDetail.First().UploadedFileId;

        return new FileProcessingContext
        {
            Discipline = discipline,
            EventId = eventId.Value,
            UploadedFileId = uploadedFileId
        };
    }

    private static async Task<Result<int>> ProcessEventingCompetitiveDisciplineAsync(int eventId, int uploadedFileId,
        int disciplineId, IReadRepository<object> readRepository, IWriteRepository<object> writeRepository,
        DbTransaction transaction,
        ICollection<UploadedFileDetailDto> uploadedFileDetail, CancellationToken cancellationToken)
    {
        var uniqueClassCategories = GetUniqueClassCategoryInfo(uploadedFileDetail);

        if (uniqueClassCategories.Count == 0)
        {
            await transaction.RollbackAsync();
            return Result<int>.Failure("No valid class categories found in the uploaded file.",
                ErrorType.BadRequest);
        }

        foreach (var uniqueClassCategory in uniqueClassCategories)
        {
            var additionalCompetitionData = JsonSerializer.Serialize(
                new
                {
                    uniqueClassCategory.ShowJumpingBeforeXc,
                    uniqueClassCategory.FirstHiOrder,
                    uniqueClassCategory.SecondHiOrder,
                    uniqueClassCategory.IsChampionship
                },
                JsonOptions);

            var competitionId =
                await InsertResultCompetitionAsync(
                    new CompetitionInfo(uniqueClassCategory.ClassCategoryName,
                        uniqueClassCategory.ClassName,
                        disciplineId, eventId, uploadedFileId, DateTime.UtcNow, //Todo: Parse actual date
                        DateTime.UtcNow, additionalCompetitionData),
                    new RepositoryContext(readRepository,
                        writeRepository, transaction), cancellationToken
                );

            await EventingDisciplineHelper.InsertResultCompetitionOfficial(competitionId, uniqueClassCategory.Official,
                writeRepository, readRepository, transaction, cancellationToken);

            var rounds = new[] { "Dressage", "Cross Country", "Show Jumping" };

            foreach (var round in rounds)
            {
                var competitionRoundId = await EventingDisciplineHelper.InsertResultCompetitionRound(
                    uniqueClassCategory.ClassDate,
                    competitionId, round,
                    readRepository, writeRepository, transaction, cancellationToken);

                var userIdWithParticipantId = await InsertResultCompetitionParticipants(uploadedFileDetail,
                    competitionRoundId, uniqueClassCategory.ClassCategoryName, uniqueClassCategory.ClassName, ClassKeyType.Eventing,
                    writeRepository, transaction, cancellationToken, true);

                await InsertResultCompetitionAsset(userIdWithParticipantId, uploadedFileDetail,
                    uniqueClassCategory.ClassCategoryName, uniqueClassCategory.ClassName, ClassKeyType.Eventing,
                    writeRepository, transaction, cancellationToken, isEventing: true);

                var userIdToResultIdMapping = await InsertResultCompetitionResults(userIdWithParticipantId,
                    writeRepository, transaction, cancellationToken);

                await EventingDisciplineHelper.InsertResultCompetitionResultData(userIdToResultIdMapping,
                    uploadedFileDetail,
                    writeRepository, transaction, uniqueClassCategory.ClassCategoryName,
                    uniqueClassCategory.ClassName, round, cancellationToken);
            }
        }

        await transaction.CommitAsync();

        return 1;
    }

    private static async Task<UploadedFileDetailDto[]> GetUploadedResultFileDataAsync(int uploadFileId,
        IReadRepository<UploadedFileDetailDto> repo, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        var uploadResultFileDetailQueryParams = new DynamicParameters();
        uploadResultFileDetailQueryParams.Add("UploadedFileId", uploadFileId);

        var items = await repo.GetListAsync(
            UploadResultFileSqlQuery,
            cancellationToken,
            uploadResultFileDetailQueryParams,
            transaction,
            commandType: QueryType.Text
        );

        var uploadedFileDetailDtos = items as UploadedFileDetailDto[] ?? items.ToArray();

        if (uploadedFileDetailDtos.Length == 0)
        {
            throw new CustomValidationException(
                "No data found for the provided upload file. Please verify the file ID and ensure the file contains valid data.");
        }

        foreach (var item in uploadedFileDetailDtos)
        {
            item.PopulateDynamicProperties();
        }

        return uploadedFileDetailDtos;
    }

    private static async Task<(string Name, int Id)> GetDisciplineAsync(IReadRepository<object> readRepository,
        int disciplineId,
        DbTransaction transaction, CancellationToken cancellationToken)
    {
        var discipline = await readRepository.QueryFirstAsync<(string Name, int Id)>(GetDisciplineSql,
            new { DisciplineId = disciplineId },
            transaction, QueryType.Text, cancellationToken);

        if (string.IsNullOrEmpty(discipline.Name))
        {
            throw new CustomValidationException($"Discipline not found with ID {disciplineId}");
        }

        return discipline;
    }

    private static List<ClassCategoryInfo> GetUniqueClassCategoryInfo(
        ICollection<UploadedFileDetailDto> uploadedFileDetailDtos)
    {
        return uploadedFileDetailDtos
            .Select(x => new ClassCategoryInfo
            {
                ClassCategoryName =
                    TryGetValueIgnoreCase(x.DynamicProperties, "ClassCategoryName", out var classCategoryName)
                        ? classCategoryName?.ToString()?.Trim() ?? string.Empty
                        : string.Empty,
                ClassName = TryGetValueIgnoreCase(x.DynamicProperties, "ClassName", out var className)
                    ? className?.ToString()?.Trim() ?? string.Empty
                    : string.Empty,
                Official = TryGetValueIgnoreCase(x.DynamicProperties, "Official", out var official)
                    ? official?.ToString()?.Trim() ?? string.Empty
                    : string.Empty,
                ClassDate = TryGetValueIgnoreCase(x.DynamicProperties, "ClassDate", out var classDate)
                    ? classDate?.ToString()?.Trim() ?? string.Empty
                    : string.Empty,
                ShowJumpingBeforeXc = TryGetValueIgnoreCase(x.DynamicProperties, "ShowJumping_Before_XC", out var value)
                    ? value?.ToString()?.Trim() ?? string.Empty
                    : string.Empty,
                FirstHiOrder = TryGetValueIgnoreCase(x.DynamicProperties, "First_HI_Order", out var value1)
                    ? value1?.ToString()?.Trim() ?? string.Empty
                    : string.Empty,
                SecondHiOrder = TryGetValueIgnoreCase(x.DynamicProperties, "Second_HI_Order", out var value2)
                    ? value2?.ToString()?.Trim() ?? string.Empty
                    : string.Empty,
                IsChampionship = TryGetValueIgnoreCase(x.DynamicProperties, "IsChampionship", out var value3)
                    ? value3?.ToString()?.Trim() ?? string.Empty
                    : string.Empty,
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.ClassCategoryName) &&
                           !string.IsNullOrWhiteSpace(item.ClassName))
            .DistinctBy(x => (x.ClassCategoryName, x.ClassName))
            .OrderBy(x => x.ClassCategoryName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ClassName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static async Task CheckFileAlreadyConfirmed(int uploadFileId, IReadRepository<object> readRepo,
        IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string checkSql = """
                                SELECT COUNT(1)
                                FROM ResultCompetition C
                                WHERE C.UploadedFileId = @UploadedFileId AND C.IsDeleted = 0
                                """;

        var existingCount = await readRepo.GetSingleAsync<int>(
            checkSql,
            new { UploadedFileId = uploadFileId },
            transaction,
            cancellationToken);

        if (existingCount > 0)
        {
            throw new CustomValidationException(
                "The uploaded file has already been confirmed.");
        }
    }

    private static async Task InsertResultCompetitionAsset(
        List<(int CompetitionParticipantId, int UserId)> participantIds,
        ICollection<UploadedFileDetailDto> uploadedFileDetail,
        string? classCategoryName,
        string className,
        string classKey,
        IWriteRepository<object> writeRepo,
        IDbTransaction transaction,
        CancellationToken cancellationToken,
        bool isEventing = false)
    {
        if (participantIds.Count == 0)
        {
            return;
        }

        var participantAssetData = ExtractParticipantAssetData(
            uploadedFileDetail,
            participantIds,
            classCategoryName,
            className,
            classKey,
            isEventing);

        if (participantAssetData.Count == 0)
        {
            return;
        }

        await BulkInsertCompetitionAssetsWithAssetResolution(
            participantAssetData,
            writeRepo,
            transaction,
            cancellationToken);
    }

    private static async Task<int> BulkInsertCompetitionAssetsWithAssetResolution(
        List<ParticipantAssetData> participantAssetData,
        IWriteRepository<object> writeRepo,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (participantAssetData.Count == 0)
        {
            return 0;
        }

        const int batchSize = 1000;


        if (participantAssetData.Count > batchSize)
        {
            var totalInserted = 0;
            for (var i = 0; i < participantAssetData.Count; i += batchSize)
            {
                var remainingCount = participantAssetData.Count - i;
                var currentBatchSize = Math.Min(batchSize, remainingCount);
                var batch = participantAssetData.Skip(i).Take(currentBatchSize).ToList();

                totalInserted += await BulkInsertCompetitionAssetsWithAssetResolution(
                    batch, writeRepo, transaction, cancellationToken);
            }

            return totalInserted;
        }

        try
        {
            var participantAssetValues = participantAssetData.Select((_, index) =>
                $"(@CompetitionParticipantId{index}, @AssetReference{index})");

            var bulkInsertWithResolutionSql = $"""
                                               WITH ParticipantAssetInput(CompetitionParticipantId, AssetReference) AS (
                                                   SELECT * FROM (VALUES 
                                                       {string.Join(",\n ", participantAssetValues)}
                                                   ) AS InputData(CompetitionParticipantId, AssetReference)
                                               ),
                                               ResolvedAssets AS (
                                                   SELECT 
                                                       pai.CompetitionParticipantId,
                                                       ar.AssetId,
                                                       ar.AssetTypeId,
                                                       pai.AssetReference
                                                   FROM ParticipantAssetInput pai
                                                   INNER JOIN AssetRegisters ar ON ar.AssetReference = pai.AssetReference
                                               )
                                               INSERT INTO ResultCompetitionAssets (CompetitionParticipantId, AssetId, AssetType)
                                               SELECT 
                                                   ra.CompetitionParticipantId,
                                                   ra.AssetId,
                                                   ra.AssetTypeId
                                               FROM ResolvedAssets ra
                                               LEFT JOIN ResultCompetitionAssets existing ON 
                                                   existing.CompetitionParticipantId = ra.CompetitionParticipantId 
                                                   AND existing.AssetId = ra.AssetId
                                               WHERE existing.CompetitionAssetId IS NULL  -- Prevent duplicates
                                               """;

            var insertParams = new DynamicParameters();
            for (var i = 0; i < participantAssetData.Count; i++)
            {
                var data = participantAssetData[i];
                insertParams.Add($"CompetitionParticipantId{i}", data.CompetitionParticipantId);
                insertParams.Add($"AssetReference{i}", data.AssetReference);
            }

            var rowsAffected = await writeRepo.ExecuteAsync(
                bulkInsertWithResolutionSql,
                cancellationToken,
                insertParams,
                transaction,
                QueryType.Text);

            return rowsAffected;
        }
        catch (Exception ex)
        {
            throw new CustomValidationException(
                $"Failed to insert competition assets: {ex.Message}");
        }
    }

    private static List<ParticipantAssetData> ExtractParticipantAssetData(
        ICollection<UploadedFileDetailDto> uploadedFileDetail,
        List<(int CompetitionParticipantId, int UserId)> participantIds,
        string? classCategoryName,
        string className,
        string classKey,
        bool isEventing = false)
    {
        List<ParticipantAssetData> participantAssetData = [];

        var relevantDtos = uploadedFileDetail
            .Where(dto => IsValidParticipantDtoWithHorseKey(dto, className, classKey, classCategoryName, isEventing))
            .ToList();

        foreach (var (participantId, userId) in participantIds)
        {
            var userDto = relevantDtos
                .FirstOrDefault(dto => dto.UserId == userId && dto.Marker == false);

            if (userDto is null)
            {
                continue;
            }

            userDto.Marker = true;

            if (!TryGetHorseAssetReference(userDto.DynamicProperties, out var assetReference) ||
                string.IsNullOrWhiteSpace(assetReference))
            {
                continue;
            }

            var participantAsset = new ParticipantAssetData
            {
                CompetitionParticipantId = participantId,
                AssetReference = assetReference
            };

            participantAssetData.Add(participantAsset);
        }
        
        relevantDtos.ForEach(dto => dto.Marker = false);
        
        return participantAssetData;
    }

    private static bool TryGetHorseAssetReference(
        Dictionary<string, string?> dynamicProperties,
        out string? assetReference)
    {
        assetReference = null;
        if (dynamicProperties.Count == 0)
            return false;

        assetReference = dynamicProperties["Horse ID"]?.Trim();
        return !string.IsNullOrWhiteSpace(assetReference);
    }

    private static bool IsValidParticipantDtoWithHorseKey(UploadedFileDetailDto dto,
        string className,
        string classKey,
        string? classCategoryName,
        bool isEventing)
    {
        return IsValidParticipantDto(dto, className, classKey, classCategoryName, isEventing) &&
               dto.DynamicProperties.Keys.Any(key => key.Equals("Horse ID"));
    }

    private static bool TryGetValueIgnoreCase(Dictionary<string, string?> dictionary, string key,
        out object? actualValue)
    {
        actualValue = null;

        if (string.IsNullOrEmpty(key) || dictionary.Count == 0)
            return false;

        //var normalizedSearchKey = key.Replace(" ", "");

        foreach (var kvp in dictionary)
        {
            if (string.IsNullOrEmpty(kvp.Key))
                continue;

            //var keyWithoutSpaces = kvp.Key.Replace(" ", "");
            // if (keyWithoutSpaces.Length != normalizedSearchKey.Length)
            //     continue;

            if (!string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase)) continue;
            actualValue = kvp.Value;
            return true;
        }

        return false;
    }

    public static int DetermineDataType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 5;

        if (int.TryParse(value, out _))
            return 0;

        if (decimal.TryParse(value, out _))
            return 1;

        if (DateTime.TryParse(value, out _))
            return 3;

        if (value.Trim().StartsWith('{') && value.Trim().EndsWith('}'))
            return 7;

        if (value.Trim().StartsWith('[') && value.Trim().EndsWith(']'))
            return 8;

        // Default to text (DataType: 5)
        return 5;
    }

    private static async Task<List<(int UserId, int CompetitionResultId)>> InsertResultCompetitionResults(
        List<(int CompetitionParticipantId, int UserId)> participantIds,
        IWriteRepository<object> writeRepo,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (participantIds.Count == 0)
        {
            return [];
        }

        var participantIdsList = participantIds.Select(x => x.CompetitionParticipantId).ToList();

        const int batchSize = 1000;

        if (participantIdsList.Count <= batchSize)
        {
            var singleBatchResult = await ProcessCompetitionResultsBatch(
                participantIdsList,
                participantIds,
                writeRepo,
                transaction,
                cancellationToken);

            return singleBatchResult;
        }

        var totalBatches = checked((int)Math.Ceiling((double)participantIdsList.Count / batchSize));
        List<(int UserId, int CompetitionResultId)> userIdToResultIdMapping = [];

        for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            var batchStart = batchIndex * batchSize;
            var currentBatchSize = Math.Min(batchSize, participantIdsList.Count - batchStart);
            var batchParticipantIds = participantIdsList.Skip(batchStart).Take(currentBatchSize).ToList();

            try
            {
                var batchParticipantMapping = participantIds
                    .Where(x => batchParticipantIds.Contains(x.CompetitionParticipantId))
                    .ToList();

                var batchResult = await ProcessCompetitionResultsBatch(
                    batchParticipantIds,
                    batchParticipantMapping,
                    writeRepo,
                    transaction,
                    cancellationToken);

                userIdToResultIdMapping.AddRange(batchResult);
            }
            catch (Exception ex)
            {
                throw new CustomValidationException(
                    $"Failed to process competition results batch {batchIndex + 1}/{totalBatches} " +
                    $"(ParticipantIds: {batchParticipantIds.Count}): {ex.Message}");
            }
        }

        return userIdToResultIdMapping;
    }

    private static async Task<List<(int UserId, int CompetitionResultId)>> ProcessCompetitionResultsBatch(
        List<int> participantIdsList,
        List<(int CompetitionParticipantId, int UserId)> participantIds,
        IWriteRepository<object> writeRepo,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (participantIdsList.Count == 0)
        {
            return [];
        }

        try
        {
            var valuesClauses = participantIdsList.Select((_, index) =>
                $"(@CompetitionParticipantId{index}, @ResultType)");

            var bulkInsertSql = $"""
                                 INSERT INTO ResultCompetitionResults (CompetitionParticipantId, ResultType)
                                 OUTPUT INSERTED.CompetitionResultId, INSERTED.CompetitionParticipantId
                                 VALUES {string.Join(", ", valuesClauses)}
                                 """;

            var insertParams = new DynamicParameters();
            insertParams.Add("ResultType", 1);

            for (var i = 0; i < participantIdsList.Count; i++)
            {
                insertParams.Add($"CompetitionParticipantId{i}", participantIdsList[i], DbType.Int32);
            }

            var insertedResults =
                await writeRepo.ExecuteQueryAsync<(int CompetitionResultId, int CompetitionParticipantId)>(
                    bulkInsertSql,
                    cancellationToken,
                    insertParams,
                    transaction,
                    QueryType.Text);

            var resultList = insertedResults?.ToList() ?? [];

            List<(int UserId, int CompetitionResultId)> userIdToResultIdList = [];

            foreach (var result in resultList)
            {
                var participant = participantIds
                    .FirstOrDefault(p => p.CompetitionParticipantId == result.CompetitionParticipantId);

                if (participant != default)
                {
                    userIdToResultIdList.Add((participant.UserId, result.CompetitionResultId));
                }
            }

            return userIdToResultIdList;
        }
        catch (Exception ex) when (ex is not CustomValidationException)
        {
            throw new CustomValidationException(
                $"Unexpected error during competition results batch insert: {ex.Message}");
        }
    }

    private static async Task<List<(int CompetitionParticipantId, int UserId)>> InsertResultCompetitionParticipants(
        ICollection<UploadedFileDetailDto> uploadedFileDetail, int competitionRoundId, string? classCategoryName,
        string className, string classKey, IWriteRepository<object> writeRepo, IDbTransaction transaction,
        CancellationToken cancellationToken, bool isClassCategoryBased = false)
    {
        var userIds = uploadedFileDetail!
            .Where(dto => IsValidParticipantDto(dto, className, classKey, classCategoryName, isClassCategoryBased))
            .Select(dto => dto.UserId)
            .ToArray();

        if (userIds.Length == 0)
        {
            return [];
        }

        const int batchSize = 1000;

        if (userIds.Length <= batchSize)
        {
            var singleBatchResult = await ProcessParticipantBatch(
                userIds,
                competitionRoundId,
                writeRepo,
                transaction,
                cancellationToken);

            return singleBatchResult;
        }

        List<(int CompetitionParticipantId, int UserId)> userIdToParticipantIdMapping = [];

        foreach (var batch in userIds.Chunk(batchSize))
        {
            var batchResult = await ProcessParticipantBatch(
                batch,
                competitionRoundId,
                writeRepo,
                transaction,
                cancellationToken);

            userIdToParticipantIdMapping.AddRange(batchResult);
        }

        return userIdToParticipantIdMapping;
    }

    private static bool IsValidParticipantDto(
        UploadedFileDetailDto dto,
        string className,
        string classKey,
        string? classCategoryName,
        bool isClassCategoryBased)
    {
        if (dto?.UserId <= 0 || dto?.DynamicProperties == null)
        {
            return false;
        }

        if (!TryGetValueIgnoreCase(dto.DynamicProperties, classKey, out var classValue) ||
            !string.Equals(classValue?.ToString()?.Trim(), className, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!isClassCategoryBased) return true;
        return TryGetValueIgnoreCase(dto.DynamicProperties, "ClassCategoryName", out var categoryValue) &&
               !string.IsNullOrWhiteSpace(categoryValue?.ToString()) &&
               string.Equals(categoryValue?.ToString()?.Trim(), classCategoryName, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<List<(int CompetitionParticipantId, int UserId)>> ProcessParticipantBatch(
        int[] userIds,
        int competitionRoundId,
        IWriteRepository<object> writeRepo,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
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

        return (await writeRepo.ExecuteQueryAsync<(int CompetitionParticipantId, int UserId)>(
            bulkInsertSql,
            cancellationToken,
            insertParams,
            transaction,
            QueryType.Text)).ToList();
    }

    private static async Task<int> InsertResultCompetitionAsync(CompetitionInfo competitionInfo,
        RepositoryContext repositoryContext, CancellationToken cancellationToken)
    {
        const string resolveClassIdSql = """
                                         SELECT ClassId
                                         FROM ResultClass
                                         WHERE ClassName = @ClassName
                                         """;
        var classId = await repositoryContext.ReadRepo.GetSingleAsync<int?>(
            resolveClassIdSql,
            new { ClassName = competitionInfo.ClassName },
            repositoryContext.Transaction,
            cancellationToken) ?? 0;

        if (classId is 0)
        {
            // Insert new class if not found
            const string insertClassSql = """
                                          INSERT INTO ResultClass (ClassName)
                                          VALUES (@ClassName);
                                          SELECT CAST(SCOPE_IDENTITY() AS INT);
                                          """;
            classId = await repositoryContext.WriteRepo.ExecuteScalarAsync<int>(
                insertClassSql,
                cancellationToken,
                new { ClassName = competitionInfo.ClassName },
                repositoryContext.Transaction,
                QueryType.Text);
        }

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

        var competitionId = await repositoryContext.WriteRepo
            .ExecuteScalarAsync<int>(InsertResultCompetitionSql, cancellationToken, insertResultCompetitionParams,
                repositoryContext.Transaction, QueryType.Text);

        return competitionId <= 0
            ? throw new CustomValidationException("Failed to insert ResultCompetition. Please check the provided data.")
            : competitionId;
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

        await writeRepo.ExecuteAsync(
            bulkInsertSql,
            cancellationToken,
            insertParams,
            transaction,
            QueryType.Text);
    }
}

public sealed class ClassKeyType
{
    public const string Dressage = "TestName";
    public const string Standard = "Class Name";
    public const string Eventing = "ClassCategoryName";
}

public sealed class DisciplineConstants
{
    public const string Eventing = "Eventing";
    public const string Dressage = "Dressage";
}

public class ParticipantAssetData
{
    public int CompetitionParticipantId { get; set; }
    public string AssetReference { get; set; } = string.Empty;
}