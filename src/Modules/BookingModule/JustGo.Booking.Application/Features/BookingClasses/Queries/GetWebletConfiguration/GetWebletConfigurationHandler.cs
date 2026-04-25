using System.Data;
using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using Newtonsoft.Json;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetWebletConfiguration
{

    public class GetWebletConfigurationHandler(IReadRepositoryFactory readRepository)
        : IRequestHandler<GetWebletConfigurationQuery, WebletConfigurationResponse?>
    {
        public async Task<WebletConfigurationResponse?> Handle(GetWebletConfigurationQuery request, CancellationToken cancellationToken = default)
        {
            const string sql = """
                               SELECT COALESCE(
                                   (
                                       SELECT TOP 1 j.Value
                               FROM EntitySetting es
                               CROSS APPLY OPENJSON(es.[Value]) j
                               WHERE es.ItemId = (
                                         SELECT TOP 1 ItemId 
                                         FROM SystemSettings 
                                         WHERE ItemKey = 'CLASS.WEBLET.DATACONFIG'
                                     )
                                 AND es.[Value] IS NOT NULL
                                 AND ISJSON(es.[Value]) = 1
                                 AND JSON_VALUE(j.Value,'$.WebletId') = @WebletGuid
                                   ),
                                   (
                                       SELECT weblet.[value]
                                       FROM SystemSettings ss
                                       CROSS APPLY OPENJSON(ss.Value) AS weblet
                                       WHERE ss.ItemKey = 'Class.WEBLET.DATACONFIG'
                                         AND JSON_VALUE(weblet.[value], '$.WebletId') = @WebletGuid
                                   )
                               ) AS FullWebletObject;
                               """;

            var repo = readRepository.GetLazyRepository<WebletConfigRaw>().Value;
            var parameters = new DynamicParameters();
            parameters.Add("@WebletGuid", request.WebletIdGuid, DbType.Guid);

            var rawConfig = await repo.GetAsync(sql, cancellationToken, parameters, null, QueryType.Text);
            if (rawConfig is null || string.IsNullOrWhiteSpace(rawConfig.FullWebletObject))
            {
                return null;
            }

            try
            {
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateFormatString = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'"
                };

                return JsonConvert.DeserializeObject<WebletConfigurationResponse>(
                    rawConfig.FullWebletObject,
                    settings);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
