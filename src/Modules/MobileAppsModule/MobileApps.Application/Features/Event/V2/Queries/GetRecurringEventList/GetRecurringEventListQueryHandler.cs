using System.Threading;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Application.Features.SystemSetting.Queries;
using MobileApps.Application.Features.SystemSetting.Queries.GetSystemSettings;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Event.V2.Queries.GetRecurringEventList
{
    class GetRecurringEventListQueryHandler : IRequestHandler<GetRecurringEventListQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public GetRecurringEventListQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator
            , ISystemSettingsService systemSettingsService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(GetRecurringEventListQuery request, CancellationToken cancellationToken)
        {
            //default date rand from one month behind
            if (string.IsNullOrWhiteSpace(request.EventName) && string.IsNullOrWhiteSpace(request.StartDate) && string.IsNullOrWhiteSpace(request.EndDate))
            {
                // First day of the previous month in current year
                var now = DateTime.UtcNow;
                var prevMonth = now.AddMonths(-1);
                request.StartDate = new DateTime(prevMonth.Year, prevMonth.Month, 1).ToString("yyyy-MM-dd");

                // A date far in the future (covers "greater than all data")
                request.EndDate = DateTime.MaxValue.ToString("yyyy-MM-dd");
            }

            string sql = GetRecurringEventSql();

            string sqlWhere = @"ed.Isrecurring = 1 AND s.StateId IN (21,22,56)
                 AND ed.LocationType <> 'shop'
                 AND ISNULL(ed.OwningEntityid, 0) = @ClubDocId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ClubDocId", request.ClubDocId);

            if (!string.IsNullOrEmpty(request.EventName.Trim()))
            {
                sqlWhere = sqlWhere + @" AND ed.EventName like '%'+@Filter+'%'";
                queryParameters.Add("@Filter", request.EventName);
            }

            if (string.IsNullOrEmpty(request.EndDate) && !string.IsNullOrEmpty(request.StartDate))
            {
                DateTime startDate = DateTime.Parse(request.StartDate);
                var start = startDate.ToString("yyyy-MM-dd 00:00:00");
                var end = startDate.ToString("yyyy-MM-dd 23:59:59");

                sqlWhere = sqlWhere + @" AND (od.ScheduleDate <= @EndDate  AND od.ScheduleEndDate  >= @StartDate)";

                queryParameters.Add("@StartDate", DateTime.Parse(start));
                queryParameters.Add("@EndDate", DateTime.Parse(end));
            }
            else if (!string.IsNullOrEmpty(request.EndDate) && !string.IsNullOrEmpty(request.StartDate))
            {

                DateTime startDate = DateTime.Parse(request.StartDate);
                DateTime endDate = DateTime.Parse(request.EndDate);

                var start = startDate.ToString("yyyy-MM-dd 00:00:00");
                var end = endDate.ToString("yyyy-MM-dd 23:59:59");

                sqlWhere = sqlWhere + @" AND (od.ScheduleDate <= @EndDate  AND od.ScheduleEndDate  >= @StartDate)";

                queryParameters.Add("@StartDate", DateTime.Parse(start));
                queryParameters.Add("@EndDate", DateTime.Parse(end));
            }
            sql = sql.Replace("@sqlWhere", sqlWhere);

            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");

            var eventlist = JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(JsonConvert.SerializeObject(result));


            await GetEventImage(eventlist, cancellationToken);

            return eventlist;
        }

        private static string GetRecurringEventSql()
        {
            return @";WITH EticketCTE AS
            (
                SELECT DISTINCT cb.Coursedocid
                FROM CourseBooking_Default cb
                INNER JOIN Products_Default pd 
                    ON cb.Productdocid = pd.DocId
                WHERE pd.Wallettemplateid > 0
            ),
            RankedEvents AS
            (
                SELECT 
                    ed.DocId,  
                    ed.Location,  
                    ed.EventName,  
                    ed.Address1,  
                    ed.Country,  
                    ed.EventLocation,  
                    ed.County,  
                    ed.Town,  
                    ed.Timezone,   
                    ed.RepositoryId, 

                    CASE 
                        WHEN et.Coursedocid IS NOT NULL THEN 1
                        ELSE 0
                    END AS IsETicket,

                    LEAD(ed.DocId) OVER (ORDER BY ed.DocId) AS NextId,

                    FORMAT(CAST(od.ScheduleDate AS DATE), 'yyyy-MM-dd') AS StartDate,
                    FORMAT(TRY_CAST(NULLIF(od.RecurringStartTime, '') AS DATETIME), 'hh:mm tt') AS StartTime,  
                    ed.Isrecurring,  
                    FORMAT(CAST(od.ScheduleEndDate AS DATE), 'yyyy-MM-dd') AS EndDate,  
                    FORMAT(TRY_CAST(NULLIF(od.RecurringEndTime, '') AS DATETIME), 'hh:mm tt') AS EndTime,  

                    CONCAT(FORMAT(od.ScheduleDate, 'ddd · dd MMM yyyy'), '. ',CONCAT(FORMAT(COALESCE(TRY_CAST(NULLIF(od.RecurringStartTime, '') AS DATETIME), '00:00'), 'hh:mm tt'),' ', x.abbreviation)) AS EventStartDate,

					  CONCAT(FORMAT(od.ScheduleEndDate, 'ddd · dd MMM yyyy'), '. ',CONCAT(FORMAT(COALESCE(TRY_CAST(NULLIF(od.RecurringEndTime, '') AS DATETIME), '00:00'), 'hh:mm tt'),' ', x.abbreviation)) AS EventEndDate,

                    CONVERT(DATETIME, CONVERT(NVARCHAR, od.ScheduleEndDate, 23) + ' ' + CONVERT(NVARCHAR, od.RecurringEndTime, 108), 120) AS EventFlagEndDate,
                    CONVERT(DATETIME, CONVERT(NVARCHAR, od.ScheduleDate, 23) + ' ' + CONVERT(NVARCHAR, od.RecurringStartTime, 108), 120) AS EventFlagStartDate,

                    CASE 
                        WHEN CAST(od.ScheduleDate AS DATE) = CAST(SYSUTCDATETIME() AS DATE) THEN 1
                        ELSE 0
                    END AS IsTodayEvent,

                    ROW_NUMBER() OVER (PARTITION BY ed.DocId ORDER BY od.ScheduleDate DESC) AS RowNum
                FROM events_Default ed
                INNER JOIN ProcessInfo pi ON ed.DocId = pi.PrimaryDocId
                INNER JOIN [State] s ON s.StateId = pi.CurrentStateId
                LEFT JOIN EventRecurringScheduleInterval od ON ed.DocId = od.EventDocId
                LEFT JOIN EticketCTE et ON ed.DocId = et.Coursedocid 

                OUTER APPLY
                (
                    SELECT TOP 1 gm_offset, abbreviation
                    FROM Timezone 
                    WHERE time_start <= CAST(DATEDIFF(HOUR,'1970-01-01 00:00:00', od.ScheduleDate) AS BIGINT) * 60 * 60
                        AND zone_id = ed.Timezone
                    ORDER BY time_start DESC
                ) AS x
                

                WHERE @sqlWhere
            )
            SELECT * 
            FROM RankedEvents
            WHERE RowNum = 1
            ORDER BY DocId;";
        }
        private async Task GetEventImage(IList<IDictionary<string, object>> events, CancellationToken cancellationToken)
        {
            var itemKeys = "SYSTEM.AZURESTOREROOT,CLUBPLUS.HOSTSYSTEMID,SYSTEM.SITEADDRESS,EVENT.DEFAULT_IMAGE";
            var systemSettings = await _systemSettingsService.GetSystemSettingsByMultipleItemKey(itemKeys, cancellationToken);
            var storeRoot = systemSettings?.Where(w => w.ItemKey == "SYSTEM.AZURESTOREROOT")?.Select(s => s.Value).SingleOrDefault();
            var hostMid = systemSettings?.Where(w => w.ItemKey == "CLUBPLUS.HOSTSYSTEMID")?.Select(s => s.Value).SingleOrDefault();
            var siteUrl = systemSettings?.Where(w => w.ItemKey == "SYSTEM.SITEADDRESS")?.Select(s => s.Value).SingleOrDefault();
            var eventDefaultImg = systemSettings?.Where(w => w.ItemKey == "EVENT.DEFAULT_IMAGE")?.Select(s => s.Value).SingleOrDefault();

            HttpClient _httpClient = new HttpClient();

            foreach (var evnt in events)
            {
                HttpResponseMessage response = null;
                string baseUrl = "";
                string url = "";
                try
                {
                    if (evnt["Location"].ToString().ToLower() != "virtual")
                    {
                        baseUrl = storeRoot + "/002/" + hostMid;
                        url = baseUrl + "/Repository/" + evnt["RepositoryId"] + "/" + evnt["DocId"] + "/" + evnt["Location"].ToString();
                        response = await _httpClient.GetAsync(url);
                    }
                    if (evnt["Location"].ToString().ToLower() == "virtual" || !response.IsSuccessStatusCode)
                    {
                        url = siteUrl + "/media/images/organization/EventDefaultImage/";
                        url = url + eventDefaultImg;

                    }

                }
                catch { }

                evnt["Location"] = url;
                evnt.Add("IsClassBooking", false);
                //remove 3 keys
                evnt.Remove("Directlink");
                evnt.Remove("RowNum");
                evnt.Remove("RepositoryId");
            }
        }

    }
}

