using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Result.Application.DTOs.Events;

namespace JustGo.Result.Application.Features.Events.Queries.GetSportsName;

public sealed class GetSportsNameQuery : IRequest<Result<string>>
{
    public GetSportsNameQuery()
    {
    }
}