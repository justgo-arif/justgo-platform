using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Infrastructure.HostedServices;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SignalR;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SignalR.RealTimeProgress;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.RevalidateMemberData.SportTypeStrategies;

public class TableTennisRevalidateMemberDataStrategy : IRevalidateMemberDataStrategy
{
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IUtilityService _utilityService;

    public TableTennisRevalidateMemberDataStrategy(IWriteRepositoryFactory writeRepositoryFactory,
        IReadRepositoryFactory readRepositoryFactory, IBackgroundTaskQueue backgroundTaskQueue,
        IUtilityService utilityService)
    {
        _writeRepositoryFactory = writeRepositoryFactory;
        _readRepositoryFactory = readRepositoryFactory;
        _backgroundTaskQueue = backgroundTaskQueue;
        _utilityService = utilityService;
    }

    public async Task<bool> RevalidateMemberDataAsync(int? fileId, ICollection<int> memberDataIds, string? operationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var repo = _writeRepositoryFactory.GetRepository<object>();
            var validationScopeId =
                await GetValidationScopeIdAsync(fileId, memberDataIds.FirstOrDefault(), cancellationToken);

            if (fileId is null)
            {
                foreach (var memberDataId in memberDataIds)
                {
                    var memberId = await GetMemberIdAsync(memberDataId, cancellationToken);

                    var parameters = new DynamicParameters();
                    parameters.Add("ValidationScopeId", validationScopeId);
                    parameters.Add("UploadedMemberId", memberId);
                    parameters.Add("UploadedFileId", null);

                    await repo.ExecuteUnboundedAsync(
                        "RuleEngineExecuteBulkValidation",
                        cancellationToken,
                        parameters);
                }
                
                return true;
            }
            
            if (operationId is null)
            {
                throw new ArgumentNullException(nameof(operationId),
                    "Operation ID cannot be null for bulk file-based revalidation.");
            }

            var tenantClientId = _utilityService.GetCurrentTenantClientId() ??
                                 throw new InvalidOperationException(
                                     "TenantClientId is missing. Ensure the current tenant context is properly set before revalidating member data.");

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
                        await RevalidateMemberDataInBackgroundAsync(
                            provider,
                            fileId.Value,
                            validationScopeId,
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
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred during revalidation of member data.", ex);
        }
    }

    private static async Task RevalidateMemberDataInBackgroundAsync(IServiceProvider provider, int fileId,
        int validationScopeId, string tenantClientId, string operationId,
        CancellationToken cancellationToken)
    {
        TenantContextManager.SetTenantClientId(tenantClientId);
        var writeRepoFactory = provider.GetRequiredService<IWriteRepositoryFactory>();
        var progressService = provider.GetRequiredService<IProgressTrackingService>();

        try
        {
            await progressService.SendProgressAsync(operationId, "Started revalidation process", 0, true,
                cancellationToken);
            await progressService.StartPeriodicProgressAsync(cancellationToken);

            await writeRepoFactory.GetRepository<object>().ExecuteUnboundedAsync(
                "RuleEngineExecuteBulkValidation",
                cancellationToken,
                new { ValidationScopeId = validationScopeId, UploadedFileId = fileId });

            await progressService.SendProgressAsync(operationId, "Revalidation process completed", 100, true,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await progressService.SendProgressAsync(operationId,
                "Revalidation was cancelled. No changes were made.",
                null,
                false,
                cancellationToken);
        }
        catch (SqlException sqlEx) when (ImportResultHelper.IsCancellationException(sqlEx))
        {
            await progressService.SendProgressAsync(operationId,
                "Revalidation was cancelled. No changes were made.",
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

    private async Task<int> GetMemberIdAsync(int? memberDataId, CancellationToken cancellationToken)
    {
        var repo = _readRepositoryFactory.GetRepository<object>();

        const string query = """
                             SELECT UploadedMemberId FROM ResultUploadedMemberData
                             WHERE UploadedMemberDataId = @UploadedMemberDataId
                             """;

        var memberId = await repo.GetSingleAsync<int?>(query, new { UploadedMemberDataId = memberDataId },
            null, cancellationToken: cancellationToken);

        if (memberId == null)
        {
            throw new InvalidOperationException("No member found for the provided Member Data ID.");
        }

        return memberId.Value;
    }

    private async Task<int> GetValidationScopeIdAsync(int? fileId, int? memberDataId, CancellationToken
        cancellationToken)
    {
        var repo = _readRepositoryFactory.GetRepository<object>();

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
}