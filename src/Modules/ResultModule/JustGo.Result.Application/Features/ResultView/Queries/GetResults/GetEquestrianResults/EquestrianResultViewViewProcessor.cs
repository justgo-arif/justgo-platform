using System. Text.Json;
using Dapper;
using JustGo.Authentication. Helper. Enums;
using JustGo.Authentication.Helper.  Paginations.Keyset;
using JustGo.Authentication. Infrastructure.  Utilities;
using JustGo.Authentication.Services.Interfaces.  Persistence.  Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetResults.  GetEquestrianResults;

public class EquestrianResultViewViewProcessor : ICompetitionResultViewProcessor
{
    private readonly IReadRepository<object> _readRepository;

    public EquestrianResultViewViewProcessor(IReadRepository<object> readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<Result<object>> QueryAsync(GetResultViewQuery request, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CompetitionId", request.CompetitionId);
        parameters. Add("@RoundId", request.RoundId);
        parameters.Add("@Search", request.Search ??  string.Empty);
        parameters.Add("@SortBy", request.SortBy ?? "Place");
        parameters.Add("@OrderBy", request.OrderBy ?? "ASC");
        parameters.Add("@PageNumber", request.  PageNumber);
        parameters.Add("@PageSize", request.PageSize);

        var dynamicResults = await _readRepository.GetListAsync<object>("GetEquestrianResults", 
            parameters, null, QueryType.StoredProcedure, cancellationToken);

        var processedResults = ProcessResults(dynamicResults.ToList());

        var paginatedResult = new KeysetPagedResult<IDictionary<string, object>>
        {
            Items = processedResults. results,
            TotalCount = processedResults.totalCount
        };

        return paginatedResult;
    }

    private (List<IDictionary<string, object>> results, int totalCount) ProcessResults(IList<object> dynamicResults)
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
                else if (kvp is { Key: "AdditionalData", Value: not null })
                {
                    if (string.IsNullOrEmpty(kvp.Value.ToString()))
                    {
                        continue;
                    }
                    
                    var additionalData = JsonSerializer.Deserialize<Dictionary<string, string>>(kvp.Value.ToString() ?? "{}");
                    if (additionalData == null) continue;
                    foreach (var addKvp in additionalData)
                    {
                        data[addKvp.Key] = addKvp.Value;
                    }
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