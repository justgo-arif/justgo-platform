using JustGo.Authentication.Helper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.CancelProcessCommands;

public class CancelImportMemberCommandHandler : IRequestHandler<CancelImportMemberCommand, Result<string>>
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;
    private readonly IWriteRepositoryFactory _writeRepositoryFactory;

    public CancelImportMemberCommandHandler(IReadRepositoryFactory readRepositoryFactory,
        IWriteRepositoryFactory writeRepositoryFactory)
    {
        _readRepositoryFactory = readRepositoryFactory;
        _writeRepositoryFactory = writeRepositoryFactory;
    }

    public async Task<Result<string>> Handle(CancelImportMemberCommand request,
        CancellationToken cancellationToken = default)
    {
        string? processId = string.Empty;
        try
        {
            const string sql = "SELECT CurrentProcessId FROM ResultUploadedFile WHERE UploadedFileId = @UploadedFileId";

            processId = await _readRepositoryFactory.GetRepository<object>()
                .GetSingleAsync<string>(sql, new { UploadedFileId = request.FileId }, null, cancellationToken);

            if (string.IsNullOrEmpty(processId))
            {
                return Result<string>.Failure("No active process found to cancel.", ErrorType.BadRequest);
            }

            if (LongRunningTasks.OperationIds.TryGetValue(processId, out var cancellationTokenSource))
            {
                await cancellationTokenSource.CancelAsync();
                await SetNullCurrentProcessId(request.FileId, cancellationToken);
                return "Process cancellation initiated successfully.";
            }

            return Result<string>.Failure("No active process found to cancel.", ErrorType.BadRequest);
        }
        finally
        {
            if (processId != null && LongRunningTasks.OperationIds.TryRemove(processId, out var tokenSource))
            {
                tokenSource.Dispose();
            }
        }
    }

    private async Task SetNullCurrentProcessId(int uploadedFileId, CancellationToken cancellationToken)
    {
        const string sql =
            "UPDATE ResultUploadedFile SET CurrentProcessId = NULL WHERE UploadedFileId = @UploadedFileId";

        await _writeRepositoryFactory.GetRepository<object>()
            .ExecuteAsync(sql, cancellationToken, new { UploadedFileId = uploadedFileId }, null, QueryType.Text);
    }
}