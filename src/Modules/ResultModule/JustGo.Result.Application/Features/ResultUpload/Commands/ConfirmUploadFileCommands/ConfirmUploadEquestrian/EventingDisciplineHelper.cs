using System.Data;
using System.Text.Json;
using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.UploadResultDtos;
using JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.ConfirmUploadEquestrian;
using JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.Infos;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands;

internal static class EventingDisciplineHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
    
    internal static async Task<int> InsertResultCompetitionRound(string classDate,
        int competitionId, string? round, IReadRepository<object> repo, IWriteRepository<object> writeRepo,
        IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string resolveCompetitionCategoryIdSql = """
                                                       SELECT CompetitionCategoryId
                                                       FROM ResultCompetitionCategory
                                                       WHERE CompetitionCategoryName = @CompetitionCategoryName
                                                       """;
        var competitionCategoryId = await repo.GetSingleAsync<int?>(
            resolveCompetitionCategoryIdSql,
            new { CompetitionCategoryName = round },
            transaction,
            cancellationToken) ?? 0;

        if (competitionCategoryId <= 0)
        {
            throw new CustomValidationException(
                $"Competition category not found for the provided CompetitionCategoryName {round}. Please ensure the competition category exists before confirming the upload.");
        }

        const string insertResultCompetitionRoundSql = """
                                                       INSERT INTO ResultCompetitionRounds (CompetitionId, RoundName, CompetitionCategoryId, StartDate, EndDate)
                                                       VALUES (@CompetitionId, @RoundName, @CompetitionCategoryId, @StartDate, @EndDate);
                                                       SELECT CAST(SCOPE_IDENTITY() AS INT);
                                                       """;

        var insertResultCompetitionRoundParams = new DynamicParameters();
        insertResultCompetitionRoundParams.Add("CompetitionId", competitionId);
        insertResultCompetitionRoundParams.Add("RoundName", round);
        insertResultCompetitionRoundParams.Add("CompetitionCategoryId", competitionCategoryId);
        insertResultCompetitionRoundParams.Add("StartDate", classDate);
        insertResultCompetitionRoundParams.Add("EndDate", classDate);

        var competitionRoundId = await writeRepo.ExecuteScalarAsync<int>(
            insertResultCompetitionRoundSql,
            cancellationToken,
            insertResultCompetitionRoundParams,
            transaction,
            QueryType.Text);

        if (competitionRoundId <= 0)
        {
            throw new CustomValidationException(
                "Failed to insert ResultCompetitionRound. Please check the provided data.");
        }

        return competitionRoundId;
    }

    internal static async Task InsertResultCompetitionResultData(
        List<(int UserId, int CompetitionResultId)> userIdToResultIdMapping,
        ICollection<UploadedFileDetailDto> uploadedFileDetailDtos,
        IWriteRepository<object> writeRepo,
        IDbTransaction transaction,
        string classCategoryName,
        string className,
        string roundName,
        CancellationToken cancellationToken)
    {
        var insertDataList = new List<(int CompetitionResultId, string Key, string Value, int DataType)>();

        var relevantDtos = uploadedFileDetailDtos!
            .Where(dto => dto.UserId > 0 &&
                          dto.DynamicProperties?.TryGetValue("ClassCategoryName", out var categoryValue) == true &&
                          dto.DynamicProperties.TryGetValue("ClassName", out var classValue) == true &&
                          string.Equals(categoryValue?.Trim(), classCategoryName, StringComparison.OrdinalIgnoreCase) &&
                          string.Equals(classValue?.Trim(), className, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        List<string> horseSpecificDataKeys = ["FinalStatus", "FinalStatusValue", "Place"];
        
        foreach (var (userId, competitionResultId) in userIdToResultIdMapping)
        {
            var userDto = relevantDtos
                .FirstOrDefault(dto => dto.UserId == userId && dto.Marker == false);

            if (userDto is null)
            {
                continue;
            }
            
            userDto.Marker = true;
            
            foreach (var (key, value) in userDto.DynamicProperties)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (horseSpecificDataKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
                {
                    var val = value?.Trim();
                    if (val != null)
                    {
                        var dType = EventingDisciplineHandler.DetermineDataType(val);
                        insertDataList.Add((competitionResultId, key, val, dType));
                    }

                    continue;
                }

                roundName = roundName switch
                {
                    "Cross Country" => "XCountry",
                    "Show Jumping" => "ShowJumping",
                    _ => roundName
                };

                if (roundName == "XCountry" && key.Equals("Obstacle", StringComparison.OrdinalIgnoreCase))
                {
                    var val = value?.Trim();
                    if (val != null)
                    {
                        var dType = EventingDisciplineHandler.DetermineDataType(val);
                        insertDataList.Add((competitionResultId, key, val, dType));
                    }

                    continue;
                }

                string? sanitizedValue = null;
                if (key.StartsWith(roundName, StringComparison.OrdinalIgnoreCase) && !key.Equals("ShowJumping_Before_XC", StringComparison.OrdinalIgnoreCase))
                {
                    sanitizedValue = value?.Trim();
                }

                if (sanitizedValue is null)
                {
                    continue;
                }

                var dataType = EventingDisciplineHandler.DetermineDataType(sanitizedValue);

                insertDataList.Add((competitionResultId, key, sanitizedValue, dataType));
            }
        }

        if (insertDataList.Count == 0)
        {
            return;
        }

        const int batchSize = 500;
            
        if (insertDataList.Count <= batchSize)
        {
            await EventingDisciplineHandler.ProcessResultDataBatch(insertDataList, writeRepo, transaction, cancellationToken);
            return;
        }

        var totalBatches = checked((int)Math.Ceiling((double)insertDataList.Count / batchSize));

        for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            var batchStart = batchIndex * batchSize;
            var currentBatchSize = Math.Min(batchSize, insertDataList.Count - batchStart);
            var batch = insertDataList.Skip(batchStart).Take(currentBatchSize).ToList();

            await EventingDisciplineHandler.ProcessResultDataBatch(batch, writeRepo, transaction, cancellationToken);
        }
    }
    
    internal static async Task InsertResultCompetitionOfficial(int competitionId, string officialValue,
        IWriteRepository<object> writeRepo, IReadRepository<object> readRepo, IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(officialValue))
                throw new CustomValidationException("Officials data is empty.");

            var officials = JsonSerializer.Deserialize<List<OfficialInfo>>(officialValue, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            });

            if (officials == null)
            {
                return;
            }

            foreach (var officialInfo in officials)
            {
                var userId = await ResolveOfficialToUserId(
                    officialInfo.OfficialId,
                    readRepo,
                    transaction,
                    cancellationToken);

                var additionalData = new Dictionary<string, string>(capacity: 10);

                if (officialInfo.ClassName != null)
                {
                    additionalData.Add("ClassName", officialInfo.ClassName);
                }

                if (officialInfo.ClassDate != null)
                {
                    additionalData.Add("ClassDate", officialInfo.ClassDate);
                }

                if (officialInfo.TestName != null)
                {
                    additionalData.Add("TestName", officialInfo.TestName);
                }

                if (officialInfo.CompDate != null)
                {
                    additionalData.Add("CompDate", officialInfo.CompDate);
                }

                if (officialInfo.GradeId != null)
                {
                    additionalData.Add("GradeId", officialInfo.GradeId);
                }

                if (officialInfo.Position != null)
                {
                    additionalData.Add("Position", officialInfo.Position);
                }

                var additionalDataJson = JsonSerializer.Serialize(additionalData, JsonOptions);

                var roleId = officialInfo.GetRoleIdAsInt();
                var isValidRole =
                    await ValidateCompetitionOfficialRole(roleId, readRepo, transaction, cancellationToken);

                if (!isValidRole)
                {
                    throw new CustomValidationException(
                        $"Invalid competition official role ID: {roleId}");
                }

                await InsertCompetitionOfficialRecord(
                    competitionId,
                    userId,
                    roleId,
                    additionalDataJson,
                    writeRepo,
                    transaction,
                    cancellationToken);
            }
        }
        catch (JsonException ex)
        {
            throw new CustomValidationException($"Invalid JSON format in officials data: {ex.Message}");
        }
    }
    
    private static async Task<int> ResolveOfficialToUserId(
        string officialId,
        IReadRepository<object> readRepo,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string resolveOfficialToUserSql = """
                                                SELECT UserId 
                                                FROM [USER]
                                                WHERE MemberId = @OfficialId
                                                """;

        try
        {
            var userId = await readRepo.GetSingleAsync<int?>(
                resolveOfficialToUserSql,
                new { OfficialId = officialId },
                transaction,
                cancellationToken);

            return userId ?? 0;
        }
        catch (Exception ex)
        {
            throw new CustomValidationException($"Failed to resolve official ID {officialId} to user ID: {ex.Message}");
        }
    }

    private static async Task<bool> ValidateCompetitionOfficialRole(
        int roleId,
        IReadRepository<object> readRepo,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string validateRoleSql = """
                                       SELECT COUNT(1)
                                       FROM ResultCompetitionOfficialRoles
                                       WHERE CompetitionOfficialRoleId = @RoleId
                                       """;

        try
        {
            var roleCount = await readRepo.GetSingleAsync<int>(
                validateRoleSql,
                new { RoleId = roleId },
                transaction,
                cancellationToken);

            return roleCount > 0;
        }
        catch (Exception ex)
        {
            throw new CustomValidationException(
                $"Failed to validate competition official role ID {roleId}: {ex.Message}");
        }
    }

    private static async Task InsertCompetitionOfficialRecord(
        int competitionId,
        int userId,
        int roleId,
        string additionalData,
        IWriteRepository<object> writeRepo,
        IDbTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string insertCompetitionOfficialSql = """
                                                    INSERT INTO ResultCompetitionOfficials (CompetitionId, UserId, CompetitionOfficialRoleId, AdditionalData)
                                                    VALUES (@CompetitionId, @UserId, @CompetitionOfficialRoleId, @AdditionalData)
                                                    """;

        var insertParams = new DynamicParameters();
        insertParams.Add("CompetitionId", competitionId);
        insertParams.Add("UserId", userId);
        insertParams.Add("CompetitionOfficialRoleId", roleId);
        insertParams.Add("AdditionalData", additionalData);

        try
        {
            var rowsAffected = await writeRepo.ExecuteAsync(
                insertCompetitionOfficialSql,
                cancellationToken,
                insertParams,
                transaction,
                QueryType.Text);

            if (rowsAffected != 1)
            {
                throw new CustomValidationException(
                    $"Expected 1 competition official to be inserted, but {rowsAffected} were affected.");
            }
        }
        catch (Exception ex) when (!(ex is CustomValidationException))
        {
            throw new CustomValidationException($"Failed to insert competition official record: {ex.Message}");
        }
    }
}