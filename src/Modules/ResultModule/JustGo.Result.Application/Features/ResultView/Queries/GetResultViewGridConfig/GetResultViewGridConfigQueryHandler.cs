using System.Text.Json;
using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.ResultViewDtos;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetResultViewGridConfig;

public class GetResultViewGridConfigQueryHandler : IRequestHandler<GetResultViewGridConfigQuery, Result<List<ResultViewGridColumnConfig>>>
{
    private readonly IReadRepository<object> _readRepository;

    public GetResultViewGridConfigQueryHandler(IReadRepository<object> readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<List<ResultViewGridColumnConfig>>> Handle(GetResultViewGridConfigQuery request,
        CancellationToken cancellationToken = default)
    {
        const string query = """
                             SELECT ISNULL(RDD.ResultViewConfig, '') AS ResultViewConfig
                             FROM ResultCompetition RC
                             INNER JOIN ResultDisciplines RDD ON RC.DisciplineId = RDD.DisciplineId
                             WHERE RC.CompetitionId = @CompetitionId
                             """;

        var columnParams = new DynamicParameters(new { CompetitionId = request.CompetitionId });

        var jsonString = await _readRepository.GetSingleAsync<string>(
            query, columnParams, null, cancellationToken, QueryType.Text);

        if (string.IsNullOrWhiteSpace(jsonString))
            return Result<List<ResultViewGridColumnConfig>>.Failure("Result view grid configuration not found.",
                ErrorType.NotFound);
        
        var configList = JsonSerializer.Deserialize<List<ResultViewGridColumnConfig>>(jsonString);

        return configList ?? Result<List<ResultViewGridColumnConfig>>.Failure(
            "Failed to deserialize result view grid configuration.", ErrorType.InternalServerError);
    }
}