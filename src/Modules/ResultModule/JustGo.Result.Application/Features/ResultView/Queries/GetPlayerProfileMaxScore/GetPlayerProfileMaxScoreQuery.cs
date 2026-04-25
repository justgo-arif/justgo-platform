using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Result.Application.DTOs.ResultViewDtos;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetPlayerProfileMaxScore;

public class GetPlayerProfileMaxScoreQuery : IRequest<Result<PlayerProfileMaxScoreDto>>
{
    public string MemberId { get; set; }

    public GetPlayerProfileMaxScoreQuery(string memberId)
    {
        MemberId = memberId;
    }
}
