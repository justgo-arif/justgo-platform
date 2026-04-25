using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;
using JustGo.Result.Application.Features.Common.Queries.GetUploadedMemberData;
using JustGo.Result.Application.Features.MemberUpload.Commands.RevalidateMemberCommands;
using JustGo.Result.Domain.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.Common;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.UpdateMemberDataCommands
{
    public class UpdateMemberDataCommandHandler : IRequestHandler<UpdateMemberDataCommand, Result<string>>
    {
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateMemberDataCommandHandler> _logger;
        private readonly IMediator _mediator;
        public UpdateMemberDataCommandHandler(IWriteRepositoryFactory writeRepoFactory,
            IReadRepositoryFactory readRepository, IUnitOfWork unitOfWork,
            ILogger<UpdateMemberDataCommandHandler> logger, IMediator mediator)
        {
            _writeRepository = writeRepoFactory;
            _readRepository = readRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<Result<string>> Handle(UpdateMemberDataCommand request, CancellationToken cancellationToken)
        {
            bool isAlreadyCommited = false;
            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var uploadedMemberDataId = request.UpdateMemberDataDto.Id;

                var member = await GetUploadedMemberAsync(uploadedMemberDataId, cancellationToken);
                if (member == null)
                    return Result<string>.Failure("Member not found", ErrorType.NotFound);


                var memberData = await _mediator.Send(new GetUploadedMemberDataQuery() { UploadedMemberDataId = request.UpdateMemberDataDto.Id }, cancellationToken);

                if (memberData == null)
                    return Result<string>.Failure("Member Data not found", ErrorType.NotFound);


                var (updatedJson, unmatchedKeys) =
                    ApplyJsonUpdates(memberData.MemberData, request.UpdateMemberDataDto.Updates);

 

                memberData.MemberData = updatedJson;
                member.Modified = true;


                await SaveMemberDataChangesAsync(memberData, member, transaction, cancellationToken);

                await SaveUserIdAndMemberIdAsync(memberData.UploadedMemberDataId,transaction, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                isAlreadyCommited = true;

                await UpdateMemberAdditionalInformationAsync(memberData.UploadedMemberDataId, cancellationToken);
                var memberDataAfterUpdate = await _mediator.Send(new GetUploadedMemberDataQuery() { UploadedMemberDataId = request.UpdateMemberDataDto.Id }, cancellationToken);

                var revalidateMemberData = new List<RevalidateMemberDataDto>
                {
                    new RevalidateMemberDataDto()
                    {
                        UploadedMemberDataId = memberData.UploadedMemberDataId,
                        MemberDataJson = memberDataAfterUpdate!.MemberData
                    }
                };
                await _mediator.Send(new RevalidateMemberCommand(revalidateMemberData) { FileId = member.UploadedFileId },
                    cancellationToken);

                return unmatchedKeys.Count != 0
                    ? $"Update successful. The following columns were not available and not updated: {string.Join(", ", unmatchedKeys)}"
                    : "Update successful";
            }
            catch (Exception)
            {
                if (!isAlreadyCommited)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                
                return Result<string>.Failure("An error occurred while updating member data",
                    ErrorType.InternalServerError);
            }
        }

        private async Task<ResultUploadedMember?> GetUploadedMemberAsync(int memberDataId,
            CancellationToken cancellationToken)
        {
            const string sql = """
                               SELECT um.UploadedMemberId,um.UploadedFileId, um.[MemberId], um.[MemberName], um.[Modified]
                               FROM [dbo].[ResultUploadedMember] um
                               INNER JOIN [dbo].[ResultUploadedMemberData] umd ON um.UploadedMemberId = umd.UploadedMemberId
                               WHERE um.[IsDeleted] = 0 AND umd.UploadedMemberDataId = @UploadedMemberId
                               """;

            var parameters = new DynamicParameters();
            parameters.Add("@UploadedMemberId", memberDataId);

            return await _readRepository.GetLazyRepository<ResultUploadedMember>().Value
                .GetAsync(sql, cancellationToken, parameters, null, QueryType.Text);
        }

        private static (string updatedJson, List<string> unmatchedKeys) ApplyJsonUpdates(string? originalJson,
            Dictionary<string, string> updates)
        {
            var json = originalJson ?? "{}";
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json)
                       ?? new Dictionary<string, object>();

            var unmatchedKeys = new List<string>();

            foreach (var kv in updates)
            {
                if (data.ContainsKey(kv.Key))
                    data[kv.Key] = kv.Value;
                else
                    unmatchedKeys.Add(kv.Key);
            }

            return (JsonConvert.SerializeObject(data), unmatchedKeys);
        }

        private async Task SaveMemberDataChangesAsync(ResultUploadedMemberData memberData, ResultUploadedMember member,
            DbTransaction transaction,
            CancellationToken cancellationToken)
        {
            var (updateDataSql, updateDataParams) = SQLHelper.GenerateUpdateSQLWithParameters(
                memberData,
                "UploadedMemberDataId",
                ["UploadedFileId", "UploadedMemberId"],
                tableName: "ResultUploadedMemberData");

            var (updateMemberSql, updateMemberParams) = SQLHelper.GenerateUpdateSQLWithParameters(
                member,
                "UploadedMemberId",
                ["UploadedFileId", "IsValidated", "ErrorMessage", "IsDeleted", "ErrorType"],
                tableName: "ResultUploadedMember");

            var dataRepo = _writeRepository.GetLazyRepository<ResultUploadedMemberData>().Value;
            var memberRepo = _writeRepository.GetLazyRepository<ResultUploadedMember>().Value;

            await dataRepo.ExecuteAsync(updateDataSql, cancellationToken, updateDataParams, transaction,
                QueryType.Text);
            await memberRepo.ExecuteAsync(updateMemberSql, cancellationToken, updateMemberParams, transaction,
                QueryType.Text);
        }


        private async Task SaveUserIdAndMemberIdAsync(int uploadedMemberDataId, DbTransaction transaction,
            CancellationToken cancellationToken)
        {


            var getMemberIdSql = """
                                 DECLARE @KyeName NVARCHAR(100) = 'MemberID'; 

                                 select top 1 @KyeName = vs.ValidationItemDisplayName from ResultUploadedMemberData rmd
                                 inner join ResultUploadedMember rm on rmd.UploadedMemberId = rm.UploadedMemberId
                                 inner join ResultUploadedFile rf on rf.UploadedFileId = rm.UploadedFileId
                                 inner join ValidationSchemaScope vss on vss.ValidationScopeId = rf.DisciplineId
                                 inner join ValidationSchema vs on vs.ValidationSchemaId = vss.ValidationSchemaId
                                 and rmd.UploadedMemberDataId = @UploadedMemberDataId and vs.ValidationItemName = 'MemberId'


                                 UPDATE rum set rum.Userid = u.UserId,
                                 rum.MemberId = u.MemberId
                                 FROM ResultUploadedMember rum
                                 INNER JOIN ResultUploadedMemberData rumd
                                     ON rum.UploadedMemberId = rumd.UploadedMemberId
                                 CROSS APPLY OPENJSON(rumd.MemberData)
                                     AS md
                                 INNER JOIN [User] u
                                     ON u.MemberId = md.value
                                 WHERE rumd.UploadedMemberDataId = @UploadedMemberDataId
                                   AND md.[key] = @KyeName;
                                 """;

            var queryParameters = new DynamicParameters();
            queryParameters.Add("UploadedMemberDataId", uploadedMemberDataId);
            var repo = _writeRepository.GetRepository<object>();

            await repo.ExecuteAsync(getMemberIdSql, cancellationToken,queryParameters, transaction, 
                QueryType.Text);
        }

        public async Task UpdateMemberAdditionalInformationAsync(int memberDataId,CancellationToken cancellationToken = default)
        {
            const string procedureName = "UpdateMemberDataAdditionalInformations";
            var queryParam = new DynamicParameters();
            queryParam.Add("memberDataId", memberDataId);
            queryParam.Add("FileId", null);
            queryParam.Add("DisciplineId", null);

            await _writeRepository.GetRepository<object>().ExecuteAsync(procedureName, cancellationToken, queryParam);

        }
    }
}
