using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.MemberUpload.Commands.DeleteMemberUploadFileCommands;

public class DeleteMemberUploadFileCommandHandler : IRequestHandler<DeleteMemberUploadFileCommand, Result<bool>>
{
    private readonly IWriteRepositoryFactory _writeRepository;

    public DeleteMemberUploadFileCommandHandler(IWriteRepositoryFactory writeRepository)
    {
        _writeRepository = writeRepository;
    }

    public async Task<Result<bool>> Handle(DeleteMemberUploadFileCommand request,
        CancellationToken cancellationToken = default)
    {
        const string query = """
                             UPDATE ResultUploadedFile
                             SET [IsDeleted] = 1
                             WHERE UploadedFileId = @UploadedFileId AND IsDeleted = 0
                             """;

        var affected = await _writeRepository.GetLazyRepository<object>().Value
            .ExecuteAsync(query, cancellationToken, new { UploadedFileId = request.UploadedFileId }, null, QueryType.Text);

        if (affected > 0)
        {
            return true;
        }

        return Result<bool>.Failure("No file found for the provided ID.",
            ErrorType.NotFound);
    }
}