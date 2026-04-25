using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.ResultViewDtos;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetResultViewGridConfig;

public class GetResultViewGridConfigQuery : IRequest<Result<List<ResultViewGridColumnConfig>>>
{
    public GetResultViewGridConfigQuery(int competitionId)
    {
        CompetitionId = competitionId;
    }

    public int CompetitionId { get; }
}