using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.Common.Enums;
using JustGo.Result.Application.Features.ResultUpload.Commands.CommandFactory.ResultProcessor;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetResults;

public class GetResultViewQueryHandler(IResultProcessorFactory resultFactory)
    : IRequestHandler<GetResultViewQuery, Result<object>>
{
    public async Task<Result<object>> Handle(GetResultViewQuery request, CancellationToken cancellationToken = default)
    {
        var processor = resultFactory.GetProcessor<ICompetitionResultViewProcessor>(request.SportType,
            ResultProcessType.RetrieveResults);
        return await processor.QueryAsync(request, cancellationToken);
    }
}