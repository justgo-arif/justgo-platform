using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.Common.Enums;
using JustGo.Result.Application.DTOs.ResultViewDtos;
using JustGo.Result.Application.Features.ResultUpload.Commands.CommandFactory.ResultProcessor;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetCompetitions;

public class GetSportsCompetitionsQueryHandler(IResultProcessorFactory resultFactory)
    : IRequestHandler<GetSportsCompetitionsQuery, Result<ResultCompetitionDto>>
{
    public async Task<Result<ResultCompetitionDto>> Handle(GetSportsCompetitionsQuery request,
        CancellationToken cancellationToken = default)
    {
        var processor = resultFactory.GetProcessor<ICompetitionQueryProcessor>(request.SportType,
            ResultProcessType.RetrieveCompetitions);
        return await processor.QueryAsync(request, cancellationToken);
    }
}