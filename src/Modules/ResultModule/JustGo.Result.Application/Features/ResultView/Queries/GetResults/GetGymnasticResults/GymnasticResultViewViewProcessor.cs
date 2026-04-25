using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetResults.GetGymnasticResults;

public class GymnasticResultViewViewProcessor : ICompetitionResultViewProcessor
{
    private readonly IReadRepository<object> _readRepository;

    public GymnasticResultViewViewProcessor(IReadRepository<object> readRepository)
    {
        _readRepository = readRepository;
    }
    public async Task<Result<object>> QueryAsync(GetResultViewQuery request, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompetitionId", request.CompetitionId);
        parameters. Add("@RoundId", request.RoundId);
        parameters.Add("@Search", request.Search ??  string.Empty);
        parameters.Add("@SortBy", request.SortBy ?? "Final Score");
        parameters.Add("@OrderBy", request.OrderBy ?? "DESC");
        parameters.Add("@PageNumber", request.  PageNumber);
        parameters.Add("@PageSize", request.PageSize);
        parameters.Add("@FiltersJson", request.FilterJson ?? string.Empty);

        var dynamicResults = await _readRepository.GetListAsync<object>("GetGymnasticsResults", 
            parameters, null, QueryType.StoredProcedure, cancellationToken);

        var processedResults = ProcessResults(dynamicResults.ToList());

        var paginatedResult = new KeysetPagedResult<IDictionary<string, object>>
        {
            Items = processedResults. results,
            TotalCount = processedResults.totalCount
        };

        return paginatedResult;
    }
    
    private static (List<IDictionary<string, object>> results, int totalCount) ProcessResults(IList<object> dynamicResults)
    {
        var processedResults = new List<IDictionary<string, object>>();
        int totalCount = 0;

        foreach (var item in dynamicResults)
        {
            var itemDict = (IDictionary<string, object>)item;
            var data = new Dictionary<string, object>();

            foreach (var kvp in itemDict)
            {
                if (kvp.Key == "TotalCount")
                {
                    totalCount = Convert.ToInt32(kvp.Value ??  0);
                }
                else
                {
                    data[kvp.Key] = kvp.Value ?? string.Empty;
                }
            }

            processedResults.Add(data);
        }

        return (processedResults, totalCount);
    }
}