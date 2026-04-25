using System.Threading;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Application.Features.SystemSetting.Queries;
using MobileApps.Application.Features.SystemSetting.Queries.GetSystemSettings;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Event.V2.Queries.GetEventListPaging
{
    class GetEventListQueryHandler : IRequestHandler<GetEventListPagingQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public GetEventListQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator
            , ISystemSettingsService systemSettingsService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(GetEventListPagingQuery request, CancellationToken cancellationToken)
        {
            
            string sql = GetEventListQL();
            string sqlWhere = @"[State].StateId IN (21, 22, 56)  AND ed.Isrecurring <> 1  
            AND ed.LocationType <> 'shop'
            AND ISNULL(ed.OwningEntityid, 0) = @ClubDocId ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ClubDocId", request.ClubDocId);

            if (!string.IsNullOrEmpty(request.EventName.Trim()))
            {
                sqlWhere = sqlWhere + @" AND ed.EventName Like '%'+@Filter+'%'";
                queryParameters.Add("@Filter", request.EventName);
            }


            if (string.IsNullOrEmpty(request.EndDate) && !string.IsNullOrEmpty(request.StartDate))
            {
                DateTime startDate = DateTime.Parse(request.StartDate);
          
                sqlWhere = sqlWhere + @" AND (CAST(ed.StartDate as Date)<= CAST(@EndDate as Date) AND CAST(ed.EndDate as Date) >= CAST(@StartDate as Date))";
                queryParameters.Add("@StartDate", startDate);
                queryParameters.Add("@EndDate", startDate);
            }
            else if (!string.IsNullOrEmpty(request.EndDate) && !string.IsNullOrEmpty(request.StartDate))
            {
                DateTime startDate = DateTime.Parse(request.StartDate);
                DateTime endDate = DateTime.Parse(request.EndDate);

               
                sqlWhere = sqlWhere + @" AND (CAST(ed.StartDate as Date)<= CAST(@EndDate as Date) AND CAST(ed.EndDate as Date) >= CAST(@StartDate as Date))";
                queryParameters.Add("@StartDate", startDate);
                queryParameters.Add("@EndDate", endDate);
            }

            queryParameters.Add("@nextId", request.NextId<=0? 0: (request.NextId+1));
            queryParameters.Add("@dataSize", (request.NextId+request.DataSize));

            sql = sql.Replace("@sqlWhere", sqlWhere);
            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");

            var eventList = JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(JsonConvert.SerializeObject(result));

            if(eventList!=null && eventList.Count>0) await GetEventLocation(eventList, cancellationToken);

            return eventList;
        }

        private static string GetEventListQL()
        {
            return @";WITH EticketCTE AS
            (
                SELECT DISTINCT cb.Coursedocid
                FROM CourseBooking_Default cb
                INNER JOIN Products_Default pd 
                    ON cb.Productdocid = pd.DocId
                WHERE pd.Wallettemplateid > 0
            ),
            TotalDataList AS (
                SELECT  
                    ed.DocId,  
                    ed.[Location],  
                    ed.Postcode,  
                    ed.EventName,  
                    ed.Address1,  
                    ed.Country,  
                    ed.EventLocation,  
                    ed.County,  
                    ed.Town,  
                    CASE 
                        WHEN et.Coursedocid IS NOT NULL THEN 1 ELSE 0
                    END AS IsETicket,
                    LEAD(ed.DocId) OVER (ORDER BY ed.DocId) AS NextId,
                    FORMAT(ed.StartDate ,'yyyy-MM-dd') AS StartDate,  
                    FORMAT(TRY_CAST(NULLIF(ed.StartTime, '') AS DATETIME), 'hh:mm tt')  AS StartTime,
                    ISNULL(ed.Isrecurring, 0) AS Isrecurring,
                    FORMAT(ed.EndDate ,'yyyy-MM-dd') AS EndDate,  
                    FORMAT(TRY_CAST(NULLIF(ed.EndTime, '') AS DATETIME), 'hh:mm tt') as EndTime,
                    ed.RepositoryId,  
                    CONCAT(FORMAT(ed.StartDate, 'yyyy-MM-dd'), ' ',CONCAT(FORMAT(COALESCE(TRY_CAST(NULLIF(ed.StartTime, '') AS DATETIME), '00:00'), 'hh:mm tt'),' ', x.abbreviation)) AS EventStartDate,
                    CONCAT(FORMAT(ed.EndDate, 'yyyy-MM-dd'), ' ',CONCAT(FORMAT(COALESCE(TRY_CAST(NULLIF(ed.EndTime, '') AS DATETIME), '00:00'), 'hh:mm tt'),' ', x.abbreviation)) AS EventEndDate,
                    DATEADD(SECOND, 0, CAST(ed.StartDate AS DATETIME) + CAST(ed.StartTime AS DATETIME)) AS EventFlagStartDate,
                    DATEADD(SECOND, 0, CAST(ed.EndDate AS DATETIME) + CAST(ed.EndTime AS DATETIME)) AS EventFlagEndDate,
                    CASE 
                        WHEN CAST(SYSUTCDATETIME() AS DATE) > CAST(ed.EndDate AS DATE) THEN -1
                        WHEN CAST(SYSUTCDATETIME() AS DATE) BETWEEN CAST(ed.StartDate AS DATE) AND CAST(ed.EndDate AS DATE) THEN 1
                        ELSE 0
                    END AS IsTodayEvent,
                    x.gm_offset as gm_offset,
                    ed.Timezone,
                    COUNT(*) OVER() AS TotalCount,
                    ROW_NUMBER() OVER (
                        ORDER BY 
                            CASE 
                                WHEN CAST(SYSUTCDATETIME() AS DATE) > CAST(ed.EndDate AS DATE) THEN -1
                                WHEN CAST(SYSUTCDATETIME() AS DATE) BETWEEN CAST(ed.StartDate AS DATE) AND CAST(ed.EndDate AS DATE) THEN 1
                                ELSE 0
                            END DESC,
                            ed.StartDate,
                            ed.StartTime
                    ) AS RowNum
                FROM Events_Default ed
                INNER JOIN ProcessInfo 
                    ON ed.DocId = ProcessInfo.PrimaryDocId  
                INNER JOIN [state] 
                    ON [State].StateId = ProcessInfo.CurrentStateId  
                LEFT JOIN EticketCTE et
                    ON et.Coursedocid = ed.DocId
                OUTER APPLY (
                    SELECT TOP 1 gm_offset, abbreviation
                    FROM Timezone
                    WHERE time_start <= CAST(DATEDIFF(HOUR, '1970-01-01', ed.StartDate) AS BIGINT) * 3600
                      AND zone_id = ed.Timezone
                    ORDER BY time_start DESC
                ) AS x
                WHERE @sqlWhere
            )
            SELECT * 
            FROM TotalDataList
            WHERE RowNum BETWEEN @nextId AND @dataSize";
        }
        private Task GetEventLocation(IList<IDictionary<string, object>> events, CancellationToken cancellationToken)
        {


            foreach (var evnt in events)
            {
                evnt["EventLocation"] = evnt["County"] + "," + evnt["Postcode"] + "," + evnt["Country"];
                //remove 2 keys
                evnt.Remove("Directlink");
                evnt.Remove("RepositoryId");
            }

            return Task.CompletedTask;
        }

    }
}
