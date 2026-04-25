using System.Data;
using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.Common.Enums;
using static JustGo.Authentication.Infrastructure.Logging.AuditLogSink;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.UpdateResultCompetitionStatus;

public class
    UpdateResultCompetitionStatusCommandHandler : IRequestHandler<UpdateResultCompetitionStatusCommand, Result<bool>>
{
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly IUtilityService _utilityService;

    public UpdateResultCompetitionStatusCommandHandler(IWriteRepositoryFactory writeRepositoryFactory,
        IUtilityService utilityService, IReadRepositoryFactory readRepositoryFactory)
    {
        _writeRepositoryFactory = writeRepositoryFactory;
        _utilityService = utilityService;
        _readRepositoryFactory = readRepositoryFactory;
    }

    public async Task<Result<bool>> Handle(UpdateResultCompetitionStatusCommand request,
        CancellationToken cancellationToken = default)
    {
        // var fileStatus = await GetFileStatus(request.FileId, cancellationToken);
        // switch (fileStatus)
        // {
        //     case (int)ResultCompetitionStatus.Failed:
        //         return Result<bool>.Failure(
        //             "The competition status cannot be updated because the associated file is currently in a failed state.",
        //             ErrorType.BadRequest);
        //     case (int)ResultCompetitionStatus.InProgress:
        //         return Result<bool>.Failure(
        //             "The competition status cannot be updated because the associated file is currently being processed.",
        //             ErrorType.BadRequest);
        // }

        var userId = await _utilityService.GetCurrentUserId(cancellationToken);
        const string sql = """
                           UPDATE ResultUploadedFile
                           SET CompetitionStatusId = @StatusId
                           WHERE UploadedFileId = @FileId;

                           UPDATE ResultCompetition
                           SET CompetitionStatusId = @StatusId
                           WHERE UploadedFileId = @FileId;
                           """;

        var result = await _writeRepositoryFactory.GetLazyRepository<object>().Value
            .ExecuteAsync(sql, cancellationToken, new { StatusId = request.StatusId, FileId = request.FileId },
                null,
                QueryType.Text);

        var isSuccess = result > 0;

        CustomLog.Event(AuditScheme.ResultManagement.Value,
            AuditScheme.ResultManagement.ResultUpload.Value,
            AuditScheme.ResultManagement.ResultUpload.UpdatedStatus.Value,
            userId,
            request.FileId,
            EntityType.Result,
            -1,
            nameof(ActionType.ChangedStatus),
            string.Empty
        );

        return isSuccess ? true : Result<bool>.Failure("Failed to update competition status.", ErrorType.BadRequest);
    }

    private async Task<int> GetFileStatus(int uploadFileId, CancellationToken cancellationToken)
    {
        const string query = """
                             SELECT CompetitionStatusid FROM ResultCompetition 
                             Where UploadedFileId = @UploadedFileId;;
                             """;

        var parameters = new DynamicParameters();
        parameters.Add("@UploadedFileId", uploadFileId, DbType.Int32);

        var competitionStatusId = await _readRepositoryFactory.GetLazyRepository<object>().Value
            .GetSingleAsync<int>(query, parameters, null, cancellationToken, QueryType.Text);

        return competitionStatusId;
    }
}