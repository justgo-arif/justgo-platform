using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.DeleteMember;

public class DeleteMemberCommandHandler : IRequestHandler<DeleteMemberCommand, Result<bool>>
{
    private readonly IWriteRepository<DeleteMemberCommand> _writeRepository;
    private readonly IReadRepository<DeleteMemberCommand> _readRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteMemberCommandHandler(IWriteRepository<DeleteMemberCommand> writeRepository,
        IReadRepository<DeleteMemberCommand> readRepository, IUnitOfWork unitOfWork)
    {
        _writeRepository = writeRepository;
        _readRepository = readRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteMemberCommand request, CancellationToken cancellationToken = default)
    {
        var isFileConfirmed = await IsFileConfirmed(request.FileId, cancellationToken);
        if (isFileConfirmed)
        {
            return Result<bool>.Failure("The results for this file have been confirmed and cannot be deleted.",
                ErrorType.BadRequest);
        }

        var isFileDeleted = await IsFileDeleted(request.FileId, cancellationToken);
        if (isFileDeleted)
        {
            return Result<bool>.Failure("The uploaded file has already been deleted.", ErrorType.BadRequest);
        }

        const string deleteQuery = """
                                       UPDATE ResultUploadedMember SET IsDeleted = 1 WHERE UploadedMemberId IN 
                                       (SELECT m.UploadedMemberId
                                       FROM ResultUploadedMemberData md
                                       INNER JOIN ResultUploadedMember m ON md.UploadedMemberId = m.UploadedMemberId
                                       WHERE md.UploadedMemberDataId IN @MemberDataIds)
                                   """;
        
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        var affectedRows = await _writeRepository.ExecuteAsync(deleteQuery, cancellationToken,
            new { MemberDataIds = request.MemberDataIds }, transaction,
            QueryType.Text);

        if (affectedRows != request.MemberDataIds.Count)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<bool>.Failure("Some members couldn't be deleted! Please try again.", ErrorType.BadRequest);
        }
        
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<bool> IsFileConfirmed(int fileId, CancellationToken cancellationToken)
    {
        const string query = """
                                 SELECT 1
                                 FROM ResultCompetition
                                 WHERE UploadedFileId = @FileId
                             """;

        var isConfirmed = await _readRepository.GetSingleAsync<bool>(query, new { FileId = fileId }, null,
            cancellationToken,
            QueryType.Text);

        return isConfirmed;
    }

    private async Task<bool> IsFileDeleted(int fileId, CancellationToken cancellationToken)
    {
        const string query = """
                                 SELECT IsDeleted
                                 FROM ResultUploadedFile
                                 WHERE UploadedFileId = @FileId
                             """;

        var isDeleted = await _readRepository.GetSingleAsync<bool>(query, new { FileId = fileId }, null,
            cancellationToken,
            QueryType.Text);

        return isDeleted;
    }
}