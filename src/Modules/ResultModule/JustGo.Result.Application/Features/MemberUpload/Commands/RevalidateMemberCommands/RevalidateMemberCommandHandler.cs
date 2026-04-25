using System.Data;
using System.Text.Json;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.Common.Enums;
using JustGo.Result.Application.Features.Common.Queries.GetDisciplineByFileId;
using JustGo.Result.Application.Features.MemberUpload.Helpers;
using JustGo.Result.Domain.Entities;
using JustGo.RuleEngine.Interfaces.ResultEntryValidation;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.RevalidateMemberCommands
{
    public class RevalidateMemberCommandHandler : IRequestHandler<RevalidateMemberCommand, Result<string>>
    {
        private readonly IWriteRepositoryFactory _writeRepoFactory;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;
        private readonly IEntryValidation _memberValidationService;
        private readonly IMediator _mediator;


        public RevalidateMemberCommandHandler(IWriteRepositoryFactory writeRepoFactory,
            IReadRepositoryFactory readRepository, IEntryValidation memberValidationService,
            IUtilityService utilityService, IMediator mediator)
        {
            _writeRepoFactory = writeRepoFactory;
            _readRepository = readRepository;
            _memberValidationService = memberValidationService;
            _utilityService = utilityService;
            _mediator = mediator;
        }

        public async Task<Result<string>> Handle(RevalidateMemberCommand request,
            CancellationToken cancellationToken = default)
        {
            var currentStatus = await GetCurrentStatusAsync(request.FileId, cancellationToken);
            
            if (currentStatus != FileStatus.PendingReview)
            {
                return Result<string>.Failure(
                    "Revalidation can only be initiated for files with status 'Pending Review'.",
                    ErrorType.BadRequest);
            }
            
            List<string> notFoundMembers = [];
            List<ResultUploadedMember> membersToUpdate = [];
            
            try
            {
                var scopeReferenceId =
                    await _mediator.Send(new GetDisciplineByFileIdQuery() { FileId = request.FileId },
                        cancellationToken);

                var resolvedValidationScopeDependencies = await MemberUploadHelper.ResolveValidationScopeDependency(
                    _readRepository,
                    scopeReferenceId, cancellationToken);

                foreach (var item in request.Items)
                {
                    if (string.IsNullOrWhiteSpace(item.MemberDataJson))
                    {
                        return Result<string>.Failure(
                            $"No JSON data provided for UploadedMemberDataId: {item.UploadedMemberDataId}",
                            ErrorType.BadRequest);
                    }

                    if (!IsValidJson(item.MemberDataJson))
                    {
                        return Result<string>.Failure(
                            $"Invalid JSON format provided for UploadedMemberId: {item.UploadedMemberDataId}",
                            ErrorType.BadRequest);
                    }

                    var dynamicProperties = MemberUploadHelper.PopulateDynamicProperties(item.MemberDataJson);

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

                    var uploadedMember = await GetUploadedMemberAsync(item.UploadedMemberDataId, cancellationToken);
                    if (uploadedMember == null)
                    {
                        notFoundMembers.Add(memberId);
                        continue;
                    }
                    var userId =
                        await _utilityService.GetUserIdByMemberIdAsync(memberId, cancellationToken);

                    if ( (resolvedValidationScopeDependencies.ShouldResolveValidationScope && string.IsNullOrEmpty(memberIdHeader) ) || (resolvedValidationScopeDependencies.ShouldResolveValidationScope && targetValidationScopeId == -1 ) )
                    {
                        uploadedMember.ErrorType = "Validation Failed";
                        uploadedMember.ErrorMessage = "Value does not match the required validation criteria.";
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
                            await _memberValidationService.ValidateEntryAsync(targetValidationScopeId,
                                item.MemberDataJson,
                                cancellationToken);

                        bool isRowValid = validatedData.All(v => v.IsValidItem);
                        uploadedMember.IsValidated = isRowValid;
                        uploadedMember.Modified = true;
                        if (isRowValid && string.IsNullOrEmpty(string.Join(", ",
                                validatedData.Where(e => e is { IsValidItem: true, ErrorReason.Length: > 0 }).Select(e => e.ErrorReason).Distinct())))
                        {
                            uploadedMember.ErrorType = "Validation Passed";
                            uploadedMember.ErrorMessage = null;
                        }
                        else if (isRowValid && !string.IsNullOrEmpty(string.Join(", ",
                                validatedData.Where(e => e is { IsValidItem: true, ErrorReason.Length: > 0 }).Select(e => e.ErrorReason).Distinct())))
                        {
                            uploadedMember.ErrorType = "N/A";
                            uploadedMember.ErrorMessage =
                                    string.Join(", ",
                                        validatedData.Where(e => e is { IsValidItem: true, ErrorReason.Length: > 0 }).Select(e => e.ErrorReason).Distinct());
                        }
                        else
                        {
                            uploadedMember.ErrorType = "Validation Failed";
                            uploadedMember.ErrorMessage =
                                string.Join(", ",
                                    validatedData.Where(e => e is { IsValidItem: false, ErrorReason.Length: > 0 }).Select(e => e.ErrorReason).Distinct());
                        }
                    }

                    membersToUpdate.Add(uploadedMember);
                }
                
                List<string> messageParts = [];
                
                if (membersToUpdate.Count != 0)
                {
                    var rowsAffected = await UpdateResultUploadedMemberChangesAsync(membersToUpdate, cancellationToken);
                    
                    if (rowsAffected > 0)
                    {
                        messageParts.Add($"{rowsAffected} member(s) updated successfully");
                    }
                }

                if (notFoundMembers.Count != 0)
                {
                    messageParts.Add($"Members not found: {string.Join(", ", notFoundMembers)}");
                }

                if (messageParts.Count == 0)
                {
                    return "No members were processed.";
                }

                return string.Join(". ", messageParts) + ".";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
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

        private bool IsValidJson(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return false;
            }

            try
            {
                JsonDocument.Parse(jsonString);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private string? GetMemberIdFromJson(string memberDataJson)
        {
            if (string.IsNullOrWhiteSpace(memberDataJson))
            {
                return (null);
            }

            var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(memberDataJson);
            string? memberId = jsonNode?["Member ID"]?.GetValue<string>();
            return (memberId);
        }

        private async Task<ResultUploadedMember?> GetUploadedMemberAsync(int uploadedMemberDataId,
            CancellationToken cancellationToken)
        {
            const string sql = """
                               SELECT M.[UploadedMemberId],M.UploadedFileId
                                   ,M.[MemberId]
                                   ,M.[MemberName]
                                   ,M.[IsValidated]
                                   ,M.[ErrorType]
                                   ,M.[ErrorMessage]
                                   ,M.[IsDeleted]
                                   ,M.[Modified]
                               FROM [ResultUploadedMember] M
                               INNER JOIN ResultUploadedMemberData MD ON M.UploadedMemberId = MD.UploadedMemberId
                               where IsDeleted = 0 and MD.UploadedMemberDataId = @UploadedMemberDataId
                               """;

            var parameters = new DynamicParameters();
            parameters.Add("@UploadedMemberDataId", uploadedMemberDataId);

            return await _readRepository.GetLazyRepository<ResultUploadedMember>().Value
                .GetAsync(sql, cancellationToken, parameters, null, QueryType.Text);
        }

        private async Task<int> UpdateResultUploadedMemberChangesAsync(List<ResultUploadedMember> members,
            CancellationToken cancellationToken)
        {
            
            var dataRepo = _writeRepoFactory.GetRepository<ResultUploadedMember>();
            foreach (var member in members)
            {
                var (updateDataSql, updateDataParams) = SQLHelper.GenerateUpdateSQLWithParameters(
                    member,
                    "UploadedMemberId",
                    ["UploadedFileId", "MemberName", "IsDeleted"],
                    tableName: "ResultUploadedMember");

                await dataRepo.ExecuteAsync(updateDataSql, cancellationToken, updateDataParams, null, QueryType.Text);
            }
            
            return members.Count;
        }
    }
}