using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Result.Application.DTOs.ResultViewDtos;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetCompetitions;

public interface ICompetitionQueryProcessor
{
    Task<Result<ResultCompetitionDto>> QueryAsync(GetSportsCompetitionsQuery request,
        CancellationToken cancellationToken);
}