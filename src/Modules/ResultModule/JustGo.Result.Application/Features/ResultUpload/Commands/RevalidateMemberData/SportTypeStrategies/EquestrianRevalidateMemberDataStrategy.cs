using System.Text.Json;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.HostedServices;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SignalR;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SignalR.RealTimeProgress;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands.Validators;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.RevalidateMemberData.SportTypeStrategies;

public class EquestrianRevalidateMemberDataStrategy : IRevalidateMemberDataStrategy
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IUtilityService _utilityService;

    public EquestrianRevalidateMemberDataStrategy(IBackgroundTaskQueue backgroundTaskQueue,
        IReadRepositoryFactory readRepositoryFactory, IWriteRepositoryFactory writeRepositoryFactory,
        IUtilityService utilityService)
    {
        _backgroundTaskQueue = backgroundTaskQueue;
        _readRepositoryFactory = readRepositoryFactory;
        _writeRepositoryFactory = writeRepositoryFactory;
        _utilityService = utilityService;
    }

    public async Task<bool> RevalidateMemberDataAsync(int? fileId, ICollection<int> memberDataIds, string? operationId,
        CancellationToken cancellationToken)
    {
        var validationScopeId = await GetValidationScopeIdAsync(_readRepositoryFactory, fileId,
            memberDataIds.FirstOrDefault(),
            cancellationToken);

        var config = await ImportResultHelper.GetResultUploadFieldValidationConfig(_readRepositoryFactory,
            validationScopeId, 1, cancellationToken);

        if (fileId is null)
        {
            return await RevalidateMemberDataWithoutFileAsync(memberDataIds, config, validationScopeId,
                cancellationToken);
        }
        
        if (operationId is null)
        {
            throw new ArgumentNullException(nameof(operationId),
                "Operation ID cannot be null for bulk file-based revalidation.");
        }
        
        var tenantClientId = _utilityService.GetCurrentTenantClientId() ??
                             throw new InvalidOperationException(
                                 "TenantClientId is missing. Ensure the tenant context is properly initialized before proceeding with member data revalidation.");
        

        await _backgroundTaskQueue.QueueBackgroundWorkItem(
            async (provider, queueToken) =>
            {
                if (!LongRunningTasks.OperationIds.TryGetValue(operationId, out var externalCts))
                {
                    externalCts = new CancellationTokenSource();
                    LongRunningTasks.OperationIds.TryAdd(operationId, externalCts);
                }

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(queueToken, externalCts.Token);

                try
                {
                    await RevalidateMemberDataInBackgroundAsync(
                        provider,
                        fileId.Value,
                        validationScopeId,
                        config,
                        tenantClientId,
                        operationId,
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

        return true;
    }

    private async Task<bool> RevalidateMemberDataWithoutFileAsync(
        ICollection<int> memberDataIds,
        string config,
        int validationScopeId,
        CancellationToken cancellationToken)
    {
        var memberDatas = await GetMemberDataAsync(_readRepositoryFactory, memberDataIds, cancellationToken);
        if (memberDatas.Count == 0)
        {
            return false;
        }

        IList<(int MemberId, string ErrorType, string ErrorMessage)> revalidationResult = [];
        var shouldPerformValidation = string.IsNullOrEmpty(config);

        foreach (var valueTuple in memberDatas)
        {
            var memberDataDict = JsonSerializer.Deserialize<Dictionary<string, string>>(valueTuple.MemberData);
            if (memberDataDict == null || memberDataDict.Count == 0)
            {
                continue;
            }

            if (shouldPerformValidation)
            {
                IFileDataValidator fileDataValidator = new FileDataValidator(config);
                var validationResult = fileDataValidator.ValidateRow(memberDataDict);

                if (validationResult.Count > 0)
                {
                    var combinedResult = string.Join(", ", validationResult.Select(e => e.ToString()));
                    revalidationResult.Add((valueTuple.UploadedMemberId, "Validation Failed", combinedResult));
                }
                else
                {
                    revalidationResult.Add((valueTuple.UploadedMemberId, "Validation Passed", string.Empty));
                }
            }

            await ExecuteValidationAsync(_writeRepositoryFactory, valueTuple.UploadedMemberId, validationScopeId,
                null, cancellationToken);
        }

        await UpdateMemberValidationErrorAsync(_writeRepositoryFactory, revalidationResult, cancellationToken);

        return true;
    }

    private static async Task RevalidateMemberDataInBackgroundAsync(IServiceProvider serviceProvider, int fileId,
        int validationScopeId, string config, string tenantClientId, string operationId,
        CancellationToken cancellationToken)
    {
        TenantContextManager.SetTenantClientId(tenantClientId);
        var readRepositoryFactory = serviceProvider.GetRequiredService<IReadRepositoryFactory>();
        var writeRepositoryFactory = serviceProvider.GetRequiredService<IWriteRepositoryFactory>();
        var progressService = serviceProvider.GetRequiredService<IProgressTrackingService>();

        try
        {
            await progressService.SendProgressAsync(
                operationId,
                "File validation process initiated. Preparing to validate uploaded member data.",
                0,
                cancellationToken: cancellationToken);
            await progressService.StartPeriodicProgressAsync(cancellationToken);

            await HandleFileRevalidationAsync(readRepositoryFactory, writeRepositoryFactory, fileId,
                validationScopeId, config, progressService, operationId, cancellationToken);

        }
        catch (OperationCanceledException)
        {
            await progressService.SendProgressAsync(operationId,
                "File Revalidation was cancelled. No changes were made.",
                null,
                false,
                cancellationToken);
        }
        catch (SqlException sqlEx) when (ImportResultHelper.IsCancellationException(sqlEx))
        {
            await progressService.SendProgressAsync(operationId,
                "File Revalidation was cancelled. No changes were made.",
                null,
                false,
                cancellationToken);
        }
        catch (Exception ex)
        {
            await progressService.SendProgressAsync(operationId,
                $"An error occurred during file import: {ex.Message}",
                null,
                false,
                cancellationToken);
        }
        finally
        {
            progressService.StopPeriodicProgress();
            TenantContextManager.ClearTenantClientId();
        }
    }

    private static async Task<IList<(int UploadedMemberId, string MemberData)>> GetMemberDataAsync(
        IReadRepositoryFactory readRepositoryFactory, ICollection<int> ids,
        CancellationToken cancellationToken)
    {
        const string memberDataQuery = """
                                       SELECT UploadedMemberId, MemberData FROM ResultUploadedMemberData
                                       WHERE UploadedMemberDataId in @UploadedMemberDataIds
                                       """;

        var readRepository = readRepositoryFactory.GetRepository<object>();
        var memberData = await readRepository.GetListAsync<(int UploadedMemberId, string MemberData)>(memberDataQuery,
            new { UploadedMemberDataIds = ids },
            null, cancellationToken: cancellationToken);
        return memberData.ToList();
    }

    private static async Task<int> GetValidationScopeIdAsync(IReadRepositoryFactory readRepositoryFactory, int? fileId,
        int? memberDataId, CancellationToken
            cancellationToken)
    {
        var repo = readRepositoryFactory.GetRepository<object>();

        const string query = """
                             SELECT DISTINCT f.DisciplineId
                             FROM ResultUploadedMember m
                             LEFT JOIN ResultUploadedMemberData md on m.UploadedMemberId = md.UploadedMemberId
                             LEFT JOIN ResultUploadedFile f on m.UploadedFileId = f.UploadedFileId
                             WHERE (@UploadedFileId IS NOT NULL AND f.UploadedFileId = @UploadedFileId)
                             	 OR (@UploadedMemberDataId IS NOT NULL AND md.UploadedMemberDataId = @UploadedMemberDataId)
                             """;

        var validationScopeId = await repo.GetSingleAsync<int?>(query, new
        {
            UploadedFileId = fileId,
            UploadedMemberDataId = memberDataId
        }, null, cancellationToken: cancellationToken);
        if (validationScopeId == null)
        {
            throw new InvalidOperationException("No DisciplineId found for the provided File ID or Member Data ID.");
        }

        return validationScopeId.Value;
    }

    private static async Task HandleFileRevalidationAsync(
        IReadRepositoryFactory readRepositoryFactory,
        IWriteRepositoryFactory writeRepositoryFactory,
        int fileId,
        int validationScopeId,
        string config,
        IProgressTrackingService progressService,
        string operationId,
        CancellationToken cancellationToken)
    {
        IFileDataValidator? importEquestrianResultValidator = null;
        if (!string.IsNullOrEmpty(config))
        {
            importEquestrianResultValidator = new FileDataValidator(config);
        }

        const string querySql = """
                                SELECT m.UploadedMemberId, md.MemberData
                                FROM ResultUploadedMember m
                                INNER JOIN ResultUploadedMemberData md on m.UploadedMemberId = md.UploadedMemberId
                                Where UploadedFileId = @UploadedFileId;
                                """;
        
        var memberDataList = (await readRepositoryFactory.GetRepository<object>()
                .GetListAsync<(int UploadedMemberId, string MemberData)>(
                    querySql,
                    new { UploadedFileId = fileId },
                    null,
                    QueryType.Text,
                    cancellationToken))
            .ToArray();

        IList<(int MemberId, string ErrorType, string ErrorMessage)> revalidationResult = [];
        
        await progressService.SendProgressAsync(
            operationId,
            "Starting member data revalidation...",
            10,
            cancellationToken: cancellationToken);

        var total = memberDataList.Length;
        var processed = 0;

        foreach (var memberData in memberDataList)
        {
            var memberDataDict = JsonSerializer.Deserialize<Dictionary<string, string>>(memberData.MemberData);
            if (memberDataDict == null || memberDataDict.Count == 0)
            {
                processed++;
                continue;
            }

            var validationResult = importEquestrianResultValidator?.ValidateRow(memberDataDict);

            if (validationResult?.Count > 0)
            {
                var combinedResult = string.Join(", ", validationResult.Select(e => e.ToString()));
                revalidationResult.Add((memberData.UploadedMemberId, "Validation Failed", combinedResult));
            }
            else
            {
                revalidationResult.Add((memberData.UploadedMemberId, "Validation Passed", string.Empty));
            }

            processed++;

            if (processed % 100 != 0 && processed != total) continue;
            var percent = checked((int)((processed / (double)total) * 60));
            await progressService.SendProgressAsync(
                operationId,
                $"Revalidated {processed} of {total} member records.",
                percent,
                cancellationToken: cancellationToken);
        }
        
        await UpdateMemberValidationErrorAsync(writeRepositoryFactory, revalidationResult, cancellationToken);
        
        await progressService.SendProgressAsync(
            operationId,
            "Member data revalidation in progress. Executing validation rules...",
            80,
            cancellationToken: cancellationToken);
        
        await ExecuteValidationAsync(writeRepositoryFactory, null, validationScopeId, fileId, cancellationToken);
        
        await progressService.SendProgressAsync(
            operationId,
            "Member data revalidation completed.",
            100,
            cancellationToken: cancellationToken);
    }

    private static async Task<bool> UpdateMemberValidationErrorAsync(IWriteRepositoryFactory writeRepositoryFactory,
        IList<(int MemberId, string ErrorType, string ErrorMessage)> revalidationResult,
        CancellationToken cancellationToken)
    {
        if (revalidationResult.Count == 0)
        {
            return true;
        }

        foreach (var batch in revalidationResult.Chunk(500))
        {
            await ProcessBatchUpdateAsync(writeRepositoryFactory, batch, cancellationToken);
        }

        return true;
    }

    private static async Task ProcessBatchUpdateAsync(IWriteRepositoryFactory writeRepositoryFactory,
        (int MemberId, string ErrorType, string ErrorMessage)[] batch,
        CancellationToken cancellationToken)
    {
        const string bulkUpdateSql = """
                                     UPDATE rm 
                                     SET ErrorType = temp.ErrorType,
                                         ErrorMessage = temp.ErrorMessage
                                     FROM ResultUploadedMember rm
                                     INNER JOIN (VALUES {0}) AS temp(MemberId, ErrorType, ErrorMessage)
                                         ON rm.UploadedMemberId = temp.MemberId;
                                     """;

        var valuesClauses = batch.Select((_, index) =>
            $"(@MemberId{index}, @ErrorType{index}, @ErrorMessage{index})");

        var valuesClause = string.Join(", ", valuesClauses);
        var finalSql = string.Format(bulkUpdateSql, valuesClause);

        var parameters = new Dictionary<string, object>(batch.Length * 3);
        for (int i = 0; i < batch.Length; i++)
        {
            var result = batch[i];
            parameters[$"MemberId{i}"] = result.MemberId;
            parameters[$"ErrorType{i}"] = result.ErrorType;
            parameters[$"ErrorMessage{i}"] = result.ErrorMessage;
        }

        var writeRepository = writeRepositoryFactory.GetRepository<object>();
        await writeRepository.ExecuteAsync(finalSql, cancellationToken, parameters, null, QueryType.Text);
    }

    private static async Task ExecuteValidationAsync(IWriteRepositoryFactory writeRepositoryFactory, int? memberId,
        int validationScopeId, int? fileId,
        CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("ValidationScopeId", validationScopeId);
        parameters.Add("UploadedMemberId", memberId);
        parameters.Add("UploadedFileId", fileId);

        await writeRepositoryFactory.GetRepository<object>().ExecuteAsync(
            "RuleEngineExecuteBulkValidation",
            cancellationToken, parameters, null, QueryType.StoredProcedure);
    }
}