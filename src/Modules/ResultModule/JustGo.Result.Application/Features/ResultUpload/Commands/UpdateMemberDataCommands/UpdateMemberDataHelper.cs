using System.Data.Common;
using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.UpdateMemberDataCommands;

public class UpdateMemberDataHelper
{
    internal static async Task<Result<string>> UpdateMemberDataAsync(IWriteRepositoryFactory writeRepositoryFactory,
        int id, string memberData, DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        var writeRepository = writeRepositoryFactory.GetRepository<object>();
        const string updateQuery = """
                                   UPDATE ResultUploadedMemberData
                                   SET MemberData = @MemberData
                                   WHERE UploadedMemberDataId = @UploadedMemberDataId
                                   """;
        var rowsAffected = await writeRepository.ExecuteAsync(updateQuery, cancellationToken,
            new { MemberData = memberData, UploadedMemberDataId = id }, transaction, QueryType.Text);
        return rowsAffected > 0
            ? "Member data updated successfully."
            : Result<string>.Failure("Failed to update member data.", ErrorType.InternalServerError);
    }

    internal static async Task ExecuteValidationAsync(IWriteRepositoryFactory writeRepositoryFactory, int memberId, 
        int validationScopeId, DbTransaction transaction, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("ValidationScopeId", validationScopeId);
        parameters.Add("UploadedMemberId", memberId);
        parameters.Add("UploadedFileId", null);

        await writeRepositoryFactory.GetRepository<object>().ExecuteUnboundedAsync(
            "RuleEngineExecuteBulkValidation",
            cancellationToken, parameters, transaction);
    }

    internal static async Task<FileMetaDataDto> GetFileMetaDataAsync(IReadRepositoryFactory readRepositoryFactory,
        int uploadedMemberDataId, CancellationToken cancellationToken)
    {
        const string fileIdQuery = """
                                   SELECT TOP 1 
                                          f.UploadedFileId      AS UploadedFileId,
                                          md.MemberData         AS MemberData,
                                          md.UploadedMemberId   AS UploadedMemberId,
                                          f.DisciplineId        AS DisciplineId
                                   FROM ResultUploadedMemberData md
                                   INNER JOIN ResultUploadedMember m ON md.UploadedMemberId = m.UploadedMemberId
                                   INNER JOIN ResultUploadedFile f  ON m.UploadedFileId = f.UploadedFileId
                                   WHERE md.UploadedMemberDataId = @UploadedMemberDataId;
                                   """;

        var readRepository = readRepositoryFactory.GetRepository<FileMetaDataDto>();

        var fileMetaData = await readRepository.GetAsync(
            fileIdQuery,
            cancellationToken,
            new { UploadedMemberDataId = uploadedMemberDataId },
            null,
            QueryType.Text);

        return fileMetaData ??
               throw new InvalidOperationException("Requested record was not found. Please try again later.");
    }

    internal static async Task<bool> IsFileConfirmedAsync(IReadRepositoryFactory readRepositoryFactory, int uploadFileId,
        CancellationToken cancellationToken)
    {
        const string checkSql = """
                                SELECT *
                                FROM ResultCompetition C
                                WHERE C.UploadedFileId = @UploadedFileId AND C.IsDeleted <> 1
                                """;

        var existingCount = await readRepositoryFactory.GetRepository<object>().GetSingleAsync<int>(
            checkSql,
            new { UploadedFileId = uploadFileId },
            null,
            cancellationToken);

        return existingCount > 0;
    }

    internal sealed class FileMetaDataDto
    {
        public int UploadedFileId { get; set; }
        public string MemberData { get; set; } = string.Empty;
        public int UploadedMemberId { get; set; }
        public int DisciplineId { get; set; }
    }
}