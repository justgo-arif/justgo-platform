using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.Events;
using JustGo.Result.Application.Features.Events.Queries.GetEventType;
using System.Text.Json;

public class GetEventTypeHandler : IRequestHandler<GetEventTypeQuery, List<EventTypeResponse>>
{
    private readonly LazyService<IReadRepository<EventTypeResponse>> _readRepository;

    public GetEventTypeHandler(
        LazyService<IReadRepository<EventTypeResponse>> readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<List<EventTypeResponse>> Handle(GetEventTypeQuery request, CancellationToken cancellationToken = default)
    {
           const string sql = @"
                      SELECT 
                      Id,
                      Text,
                      Caption,
                      ConfigJson
                  FROM (
                  SELECT 
                      JSON_VALUE(ss.value, '$.MyProfileConfig.Id') AS Id,
                      JSON_VALUE(ss.value, '$.MyProfileConfig.Name') AS Text,
                      JSON_VALUE(ss.value, '$.MyProfileConfig.Name') AS Caption,
                      JSON_QUERY(ss.value, '$.MyProfileConfig.ConfigJson') AS ConfigJson,
                      1 AS SortOrder
                        from [SystemSettings] ss where itemkey  = 'RESULT.SPORTTYPE'
                  and JSON_VALUE(ss.value, '$.MyProfile') = 'true' and IsNull(@IsProfile,0)=1
                  
                      UNION ALL
                      
                      SELECT 
                          rc.RecordGuid AS Id,
                          rc.TypeName AS Text,
                          rc.Description AS Caption,
                          rc.ConfigJson AS ConfigJson,
                          2 AS SortOrder
                      FROM dbo.ResultEventType rc
                      WHERE rc.IsActive = 1
                  ) AS CombinedResults
                  ORDER BY SortOrder, Id;
        ";
        var parameters = new DynamicParameters();

        parameters.Add("@IsProfile", request.IsProfile);

        var dbData = (await _readRepository.Value.GetListAsync(sql, cancellationToken, parameters, commandType: "text")).ToList();

        if (!dbData.Any())
            return new List<EventTypeResponse>();

        var response = new List<EventTypeResponse>(dbData.Count);

        foreach (var row in dbData)
        {
            var config = TryParseConfig(row.ConfigJson);

            response.Add(new EventTypeResponse
            {
                Id = row.Id,
                Text = row.Text,
                Caption = row.Caption,
                Config = config ?? new Config()
            });
        }

        return response;
    }

    private static Config? TryParseConfig(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Config>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }
        catch
        {
            return null;
        }
    }
}
