using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.Events;

namespace JustGo.Result.Application.Features.Events.Queries.GetCompetition;

public class GetCompetitionQuery : IRequest<Result<GetCompetitionResponse>>
{
    public int EventId { get; set; }
}