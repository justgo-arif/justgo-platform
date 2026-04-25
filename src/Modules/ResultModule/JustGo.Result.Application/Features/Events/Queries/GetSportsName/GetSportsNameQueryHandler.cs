using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.Events;

namespace JustGo.Result.Application.Features.Events.Queries.GetSportsName;

public class GetSportsNameQueryHandler : IRequestHandler<GetSportsNameQuery, Result<string>>
{
    private readonly IReadRepositoryFactory _readRepositoryFactory;

    public GetSportsNameQueryHandler(IReadRepositoryFactory readRepositoryFactory)
    {
        _readRepositoryFactory = readRepositoryFactory;
    }

    public async Task<Result<string>> Handle(GetSportsNameQuery request, CancellationToken cancellationToken = default)
    {
        var repository = _readRepositoryFactory.GetLazyRepository<object>().Value;

        const string sql = """
                           SELECT JSON_VALUE([Value], '$."SportName"') as SportName
                           FROM SystemSettings WHERE ItemKey = 'Result.SportType'
                           """;

        var result = await repository.QueryFirstAsync<string>(sql, null, null,
            QueryType.Text, cancellationToken: cancellationToken);

        return result ?? Result<string>.Failure("Failed to retrieve sports name.", ErrorType.NotFound);
    }
}