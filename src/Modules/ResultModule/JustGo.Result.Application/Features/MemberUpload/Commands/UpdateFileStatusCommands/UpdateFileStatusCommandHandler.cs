using System.Data;
using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.Common.Enums;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.UpdateFileStatusCommands;

public class UpdateFileStatusCommandHandler : IRequestHandler<UpdateFileStatusCommand, Result<string>>
{
    private readonly IWriteRepositoryFactory _writeRepoFactory;
    private readonly IUtilityService _utilityService;
    private readonly IReadRepositoryFactory _readRepoFactory;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateFileStatusCommandHandler(IWriteRepositoryFactory writeRepositoryFactory,
        IUtilityService utilityService, IReadRepositoryFactory readRepoFactory, IUnitOfWork unitOfWork)
    {
        _writeRepoFactory = writeRepositoryFactory;
        _utilityService = utilityService;
        _readRepoFactory = readRepoFactory;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<string>> Handle(UpdateFileStatusCommand request,
        CancellationToken cancellationToken = default)
    {
        if (request.UploadFileStatus == FileStatus.Unarchived)
        {
            return await UnarchiveFile(request.UploadFileId, cancellationToken);
        }

        var repo = _writeRepoFactory.GetRepository<object>();
        const string updateQuery = """
                                   UPDATE ResultUploadedFile
                                   SET FileStatusId = @FileStatusId
                                   WHERE UploadedFileId = @UploadedFileId;

                                   INSERT INTO ResultUploadedFileStatusLog(ResultUploadedFileStatusId, ResultUploadedFileId, UpdatedBy, UpdatedAt)
                                   VALUES(@FileStatusId, @UploadedFileId, @UpdatedBy, @UpdatedAt)
                                   """;
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var parameters = new DynamicParameters();
        parameters.Add("@UploadedFileId", request.UploadFileId, DbType.Int32);
        parameters.Add("@FileStatusId", (int)request.UploadFileStatus, DbType.Int32);
        parameters.Add("@UpdatedBy", await _utilityService.GetCurrentUserId(cancellationToken), DbType.Int32);
        parameters.Add("@UpdatedAt", DateTime.UtcNow, DbType.DateTime);

        await repo.ExecuteAsync(updateQuery, CancellationToken.None,
            parameters, transaction, QueryType.Text);
        
        await transaction.CommitAsync();

        return "File status updated successfully.";
    }

    private async Task<Result<string>> UnarchiveFile(int uploadFileId, CancellationToken cancellationToken = default)
    {
        if (!await IsFileInArchivedState(uploadFileId, cancellationToken))
        {
            return Result<string>.Failure("The file is not in an archived state and cannot be unarchived.",
                ErrorType.BadRequest);
        }
        
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var repo = _writeRepoFactory.GetRepository<object>();
        const string updateQuery = """
                                   DECLARE @ResultUploadedFileStatusId INT;

                                   SELECT @ResultUploadedFileStatusId = ResultUploadedFileStatusId
                                   FROM ResultUploadedFileStatusLog
                                   WHERE ResultUploadedFileId = @UploadedFileId
                                   ORDER BY ResultUploadedFileStatusLogId DESC
                                   OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY

                                   UPDATE ResultUploadedFile
                                   SET FileStatusId = @ResultUploadedFileStatusId
                                   WHERE UploadedFileId = @UploadedFileId;
                                   
                                   INSERT INTO ResultUploadedFileStatusLog(ResultUploadedFileStatusId, ResultUploadedFileId, UpdatedBy, UpdatedAt)
                                   VALUES(@ResultUploadedFileStatusId, @UploadedFileId, @UpdatedBy, @UpdatedAt)
                                   """;

        var parameters = new DynamicParameters();
        parameters.Add("@UploadedFileId", uploadFileId, DbType.Int32);
        parameters.Add("@UpdatedBy", await _utilityService.GetCurrentUserId(cancellationToken), DbType.Int32);
        parameters.Add("@UpdatedAt", DateTime.UtcNow, DbType.DateTime);

        var affected = await repo.ExecuteAsync(updateQuery, cancellationToken,
            parameters, transaction, QueryType.Text);
        
        await transaction.CommitAsync();
        
        if (affected > 0)
            return "File unarchived successfully.";

        return Result<string>.Failure("No file found for the provided ID.",
            ErrorType.NotFound);
    }

    private async Task<bool> IsFileInArchivedState(int uploadFileId, CancellationToken cancellationToken = default)
    {
        var repo = _readRepoFactory.GetRepository<object>();
        const string query = """
                             SELECT COUNT(1)
                             FROM ResultUploadedFile
                             WHERE UploadedFileId = @UploadedFileId AND FileStatusId = @FileStatusId
                             """;

        var parameters = new DynamicParameters();
        parameters.Add("@UploadedFileId", uploadFileId, DbType.Int32);
        parameters.Add("@FileStatusId", (int)FileStatus.Archived, DbType.Int32);

        var count = await repo.GetSingleAsync<int>(query, parameters, null, cancellationToken, QueryType.Text);

        return count > 0;
    }
}