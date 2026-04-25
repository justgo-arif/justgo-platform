using JustGo.Authentication.Infrastructure.Utilities;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetResults;

public interface ICompetitionResultViewProcessor
{
    Task<Result<object>> QueryAsync(GetResultViewQuery request,
        CancellationToken cancellationToken);
}