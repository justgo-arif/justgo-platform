using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.GetImportResultStatus;

public class GetImportResultStatusQueryHandler : IRequestHandler<GetImportResultStatusQuery, Result<string>>
{
    private readonly IReadRepository<string> _readRepository;

    public GetImportResultStatusQueryHandler(IReadRepository<string> readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<string>> Handle(GetImportResultStatusQuery request,
        CancellationToken cancellationToken = default)
    {
        const string query = """
                             SELECT fs.[Status]
                             FROM ResultUploadedFile f
                             INNER JOIN ResultUploadedFileStatus fs ON f.FileStatusId = fs.ResultUploadedFileStatusId
                             WHERE f.UploadedFileId = @FileId
                             """;

        var result = await _readRepository.GetSingleAsync<string>(query, new { FileId = request.FileId }, null,
            cancellationToken, QueryType.Text);

        return string.IsNullOrEmpty(result)
            ? Result<string>.Failure("Import result status not found.", ErrorType.InternalServerError)
            : result;
    }
}