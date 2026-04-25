using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.ResultUpload.Queries.GetImportResultStatus;

public class GetImportResultStatusQuery(int fileId) : IRequest<Result<string>>
{
    public int FileId { get; } = fileId;
}