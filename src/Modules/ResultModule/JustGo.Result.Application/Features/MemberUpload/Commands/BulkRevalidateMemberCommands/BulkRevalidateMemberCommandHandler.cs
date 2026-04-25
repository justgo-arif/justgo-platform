using System.Data;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.HostedServices;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SignalR;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.Common.Enums;
using JustGo.Result.Application.Features.Common.Queries.GetDisciplineByFileId;
using JustGo.Result.Application.Features.MemberUpload.Helpers;
using JustGo.Result.Domain.Entities;
using JustGo.RuleEngine.Interfaces.ResultEntryValidation;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.BulkRevalidateMemberCommands
{
    public class BulkRevalidateMemberCommandHandler : IRequestHandler<BulkRevalidateMemberCommand, Result<string>>
    {
         private readonly IReadRepositoryFactory _readRepository;
        private readonly IWriteRepositoryFactory _writeRepoFactory;

        // private readonly IEntryValidation _memberValidationService;
        // private readonly IMediator _mediator;
        private readonly IUtilityService _utilityService;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;

        public BulkRevalidateMemberCommandHandler(IBackgroundTaskQueue backgroundTaskQueue,
            IUtilityService utilityService, IWriteRepositoryFactory writeRepoFactory, IReadRepositoryFactory readRepository)
        {
            _backgroundTaskQueue = backgroundTaskQueue;
            _utilityService = utilityService;
            _writeRepoFactory = writeRepoFactory;
            _readRepository = readRepository;
        }

        public async Task<Result<string>> Handle(BulkRevalidateMemberCommand request,
            CancellationToken cancellationToken = default)
        {
            var currentStatus = await GetCurrentStatusAsync(request.FileId, cancellationToken);

            if (currentStatus != FileStatus.PendingReview)
            {
                return Result<string>.Failure(
                    "Revalidation can only be initiated for files with status 'Pending Review'.",
                    ErrorType.BadRequest);
            }
            
            await UpdateFileStatus(_writeRepoFactory, request.FileId, FileStatus.Revalidating);

            var tenantClientId = _utilityService.GetCurrentTenantClientId() ??
                                 throw new Exception(
                                     "Tenant information is missing. Please contact support or try again later.");

            var operationId = Guid.NewGuid().ToString("N");

            await _backgroundTaskQueue.QueueBackgroundWorkItem(async (serviceProvider, queueToken) =>
            {
                if (!LongRunningTasks.OperationIds.TryGetValue(operationId, out var externalCts))
                {
                    externalCts = new CancellationTokenSource();
                    LongRunningTasks.OperationIds.TryAdd(operationId, externalCts);
                }

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(queueToken, externalCts.Token);
                linkedCts.CancelAfter(TimeSpan.FromMinutes(90));

                try
                {
                    await ExecuteBackgroundMemberValidationAsync(
                        serviceProvider,
                        request,
                        tenantClientId,
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

        private async Task<FileStatus> GetCurrentStatusAsync(int requestFileId, CancellationToken cancellationToken)
        {
            const string sql = """
                               SELECT FileStatusId
                               FROM ResultUploadedFile
                               WHERE UploadedFileId = @UploadedFileId;
                               """;

            var parameters = new DynamicParameters();
            parameters.Add("@UploadedFileId", requestFileId, DbType.Int32);

            var statusId = await _readRepository.GetRepository<object>()
                .GetSingleAsync<int>(sql, parameters, null, cancellationToken, QueryType.Text);

            return (FileStatus)statusId;
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

        private static async Task ExecuteBackgroundMemberValidationAsync(IServiceProvider serviceProvider,
            BulkRevalidateMemberCommand request, string tenantClientId, CancellationToken cancellationToken)
        {
            TenantContextManager.SetTenantClientId(tenantClientId);

            List<string> notFoundMembers = [];
            List<ResultUploadedMember> membersToUpdate = [];
            var mediator = serviceProvider.GetRequiredService<IMediator>();
            var readRepository = serviceProvider.GetRequiredService<IReadRepositoryFactory>();
            var writeRepoFactory = serviceProvider.GetRequiredService<IWriteRepositoryFactory>();
            var memberValidationService = serviceProvider.GetRequiredService<IEntryValidation>();
            var utilityService = serviceProvider.GetRequiredService<IUtilityService>();
            var progressService = serviceProvider.GetRequiredService<IProgressTrackingService>();

            await progressService.SendProgressAsync(request.WebSocketId, "Starting file revalidating", 0,
                cancellationToken: cancellationToken);

            await progressService.StartPeriodicProgressAsync(cancellationToken);

            try
            {
                var scopeReferenceId =
                    await mediator.Send(new GetDisciplineByFileIdQuery() { FileId = request.FileId },
                        cancellationToken);

                await progressService.SendProgressAsync(request.WebSocketId, "Started file revalidating", 2,
                    cancellationToken: cancellationToken);

                var uploadedMemberDataList =
                    await GetUploadedMemberDataByFileIdAsync(readRepository, request.FileId, cancellationToken);

                if (uploadedMemberDataList.Count == 0)
                {
                    await progressService.SendProgressAsync(request.WebSocketId,
                        "No member data found to revalidate", 100,
                        cancellationToken: cancellationToken);
                    return;
                }

                await progressService.SendProgressAsync(request.WebSocketId,
                    "Fetched uploaded member data", 10,
                    cancellationToken: cancellationToken);

                var resolvedValidationScopeDependencies = await MemberUploadHelper.ResolveValidationScopeDependency(
                    readRepository,
                    scopeReferenceId, cancellationToken);

                await progressService.SendProgressAsync(request.WebSocketId,
                    "Resolved validation scope dependencies", 13,
                    cancellationToken: cancellationToken);

                int totalMembers = uploadedMemberDataList.Count;
                const int startPercent = 15;
                const int endPercent = 95;
                int processedMembers = 0;

                foreach (var memberData in uploadedMemberDataList)
                {
                    var dynamicProperties = MemberUploadHelper.PopulateDynamicProperties(memberData.MemberData);

                    var targetValidationScopeId = -1;
                    if (resolvedValidationScopeDependencies.ShouldResolveValidationScope)
                        MemberUploadHelper.ResolveValidationScopeId(dynamicProperties!,
                            resolvedValidationScopeDependencies.ValidationScopeFieldMappings,
                            resolvedValidationScopeDependencies.HeaderName, ref targetValidationScopeId);

                    targetValidationScopeId = !resolvedValidationScopeDependencies.ShouldResolveValidationScope &&
                                              targetValidationScopeId == -1
                        ? scopeReferenceId
                        : targetValidationScopeId;

                    var memberIdHeader =
                        resolvedValidationScopeDependencies.ValidatedMemberIdHeaders
                            .FirstOrDefault(m => m.ValidationScopeId == targetValidationScopeId)
                            .ValidationItemDisplayName ?? ResultUploadFields.MemberId;

                    var memberId = !string.IsNullOrEmpty(memberIdHeader)
                        ? dynamicProperties[memberIdHeader]?.Trim()
                        : string.Empty;

                    if (string.IsNullOrWhiteSpace(memberId))
                    {
                        continue;
                    }

                    var uploadedMember =
                        await GetUploadedMemberAsync(readRepository, memberData.UploadedMemberDataId,
                            cancellationToken);
                    if (uploadedMember == null)
                    {
                        notFoundMembers.Add(memberId);
                        continue;
                    }

                    uploadedMember.MemberId = memberId;
                    var userId =
                        await utilityService.GetUserIdByMemberIdAsync(uploadedMember.MemberId, cancellationToken);

                    if ( (resolvedValidationScopeDependencies.ShouldResolveValidationScope &&
                        string.IsNullOrEmpty(memberIdHeader) ) || (resolvedValidationScopeDependencies.ShouldResolveValidationScope && targetValidationScopeId == -1))
                    {
                        uploadedMember.ErrorType = "Validation Failed";
                        uploadedMember.ErrorMessage = "Value does not match the required validation criteria.";
                        uploadedMember.IsValidated = false;
                    }
                    else if (userId is null or 0)
                    {
                        uploadedMember.ErrorType = "Validation Failed";
                        uploadedMember.ErrorMessage = "Member ID does not exist";
                        uploadedMember.IsValidated = false;
                        uploadedMember.UserId = 0;
                    }
                    else
                    {
                        uploadedMember.UserId = (int)userId;
                        var validatedData =
                            await memberValidationService.ValidateEntryAsync(targetValidationScopeId,
                                memberData.MemberData, cancellationToken);

                        var isRowValid = validatedData.All(v => v.IsValidItem);
                        uploadedMember.IsValidated = isRowValid;
                        uploadedMember.Modified = true;
                        if (isRowValid && string.IsNullOrEmpty(string.Join(", ",
                                validatedData.Where(e => e.IsValidItem && e.ErrorReason.Length > 0)
                                    .Select(e => e.ErrorReason).Distinct())))
                        {
                            uploadedMember.ErrorType = "Validation Passed";
                            uploadedMember.ErrorMessage = null;
                        }
                        else if (isRowValid && !string.IsNullOrEmpty(string.Join(", ",
                                     validatedData.Where(e => e.IsValidItem && e.ErrorReason.Length > 0)
                                         .Select(e => e.ErrorReason).Distinct())))
                        {
                            uploadedMember.ErrorType = "N/A";
                            uploadedMember.ErrorMessage =
                                string.Join(", ",
                                    validatedData.Where(e => e.IsValidItem && e.ErrorReason.Length > 0)
                                        .Select(e => e.ErrorReason).Distinct());
                        }
                        else
                        {
                            uploadedMember.ErrorType = "Validation Failed";
                            uploadedMember.ErrorMessage =
                                string.Join(", ",
                                    validatedData.Where(e => !e.IsValidItem && e.ErrorReason.Length > 0)
                                        .Select(e => e.ErrorReason).Distinct());
                        }
                    }

                    processedMembers++;

                    int progress = checked(startPercent + (int)Math.Round(
                        (double)processedMembers / totalMembers * (endPercent - startPercent),
                        MidpointRounding.AwayFromZero));

                    progress = Math.Clamp(progress, 0, 100);

                    await progressService.SendProgressAsync(request.WebSocketId,
                        $"Revalidating member ID: {uploadedMember.MemberId}", progress,
                        cancellationToken: cancellationToken);

                    membersToUpdate.Add(uploadedMember);
                }

                if (membersToUpdate.Count != 0)
                {
                    var rowsAffected =
                        await UpdateResultUploadedMemberChangesAsync(writeRepoFactory, membersToUpdate,
                            cancellationToken);
                    if (rowsAffected > 0)
                    {
                        await progressService.SendProgressAsync(request.WebSocketId,
                            "Member revalidation completed", 100,
                            cancellationToken: cancellationToken);
                    }
                }

                if (notFoundMembers.Count != 0)
                {
                    await progressService.SendProgressAsync(request.WebSocketId,
                        $"The following Member IDs were not found: {string.Join(", ", notFoundMembers)}", 100,
                        cancellationToken: cancellationToken);
                }
                
                await UpdateFileStatus(writeRepoFactory, request.FileId, FileStatus.PendingReview);
            }
            catch (OperationCanceledException)
            {
                await UpdateFileStatus(writeRepoFactory, request.FileId, FileStatus.Cancelled);
                await progressService.SendProgressAsync(request.WebSocketId, "Operation cancelled by user", 100,
                    false, CancellationToken.None);
            }
            catch (SqlException sqlEx) when (IsCancellationException(sqlEx))
            {
                await UpdateFileStatus(writeRepoFactory, request.FileId, FileStatus.Cancelled);
                await progressService.SendProgressAsync(request.WebSocketId, "Operation cancelled by user", 100,
                    false, CancellationToken.None);
            }
            catch (Exception e)
            {
                await UpdateFileStatus(writeRepoFactory, request.FileId, FileStatus.Failed);
                await progressService.SendProgressAsync(request.WebSocketId,
                    $"An error occurred during revalidation: {e.Message}", 100,
                    cancellationToken: cancellationToken);
            }
            finally
            {
                progressService.StopPeriodicProgress();
                TenantContextManager.ClearTenantClientId();
            }
        }

        private static bool IsCancellationException(SqlException sqlException)
        {
            return sqlException.Number == 0 &&
                   (sqlException.Message.Contains("Operation cancelled by user", StringComparison.OrdinalIgnoreCase));
        }

        private static async Task<ResultUploadedMember?> GetUploadedMemberAsync(
            IReadRepositoryFactory readRepositoryFactory, int uploadedMemberId,
            CancellationToken cancellationToken)
        {
            const string sql = """
                               SELECT M.[UploadedMemberId]
                                   ,M.[MemberId]
                                   ,M.[MemberName]
                                   ,M.[IsValidated]
                                   ,M.[ErrorType]
                                   ,M.[ErrorMessage]
                                   ,M.[IsDeleted]
                                   ,M.[Modified]
                                   ,u.[UserId]
                               FROM [ResultUploadedMember] M
                               INNER JOIN ResultUploadedMemberData MD ON M.UploadedMemberId = MD.UploadedMemberId
                               left join [user] u on u.memberid = m.[MemberId]
                               where IsDeleted = 0 and MD.UploadedMemberDataId = @UploadedMemberDataId
                               """;

            var parameters = new DynamicParameters();
            parameters.Add("@UploadedMemberDataId", uploadedMemberId);

            return await readRepositoryFactory.GetLazyRepository<ResultUploadedMember>().Value
                .GetAsync(sql, cancellationToken, parameters, null, QueryType.Text);
        }

        private static async Task<List<ResultUploadedMemberData>> GetUploadedMemberDataByFileIdAsync(
            IReadRepositoryFactory readRepositoryFactory, int fileId,
            CancellationToken cancellationToken)
        {
            const string sql = """
                               select 
                               umd.UploadedMemberDataId,
                               umd.UploadedMemberId,
                               umd.MemberData
                               from ResultUploadedMemberData umd
                               inner join ResultUploadedMember um on umd.UploadedMemberId = um.UploadedMemberId
                               where um.UploadedFileId = @UploadedFileId and um.IsDeleted = 0
                               """;

            var parameters = new DynamicParameters();
            parameters.Add("@UploadedFileId", fileId);

            return (await readRepositoryFactory.GetLazyRepository<ResultUploadedMemberData>().Value
                .GetListAsync(sql, cancellationToken, parameters, null, QueryType.Text)).ToList();
        }

        private static async Task<int> UpdateResultUploadedMemberChangesAsync(
            IWriteRepositoryFactory writeRepositoryFactory, List<ResultUploadedMember> members,
            CancellationToken cancellationToken)
        {
            var dataRepo = writeRepositoryFactory.GetRepository<ResultUploadedMember>();
            foreach (var member in members)
            {
                var (updateDataSql, updateDataParams) = SQLHelper.GenerateUpdateSQLWithParameters(
                    member,
                    "UploadedMemberId",
                    ["UploadedFileId", "UserId", "MemberId", "MemberName", "IsDeleted"],
                    tableName: "ResultUploadedMember");

                await dataRepo.ExecuteAsync(updateDataSql, cancellationToken, updateDataParams, null, QueryType.Text);
            }

            return members.Count;
        }
    }
}