using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using MobileApps.Application.Features.Event.V2.Queries.GetRecurringOccuranceBookingDateList;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Event.V2.Queries.GetRecurringEventOccuranceList
{
    class GetRecurringEventOccurrenceListQueryHandler : IRequestHandler<GetRecurringEventOccuranceListQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public GetRecurringEventOccurrenceListQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator
            , ISystemSettingsService systemSettingsService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(GetRecurringEventOccuranceListQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT  
                    ed.DocId,
                    ed.Location,
                    ed.Postcode,
                    ed.EventName,
                    ed.Address1,
                    ed.Country,
                    ed.EventLocation,
                    ed.County,
                    ed.Town,
                    ed.OwningEntityid,
                    ed.Timezone,
                    ed.StartTime,
                    ed.EndTime,
                    ed.Isrecurring,
                    ed.RepositoryId,
                    ed.Directlink,

                    -- Times formatted as hh:mm tt
                    FORMAT(TRY_CAST(NULLIF(sl.RecurringEndTime, '') AS DATETIME), 'hh:mm tt') AS RecurringEndTime,
                    FORMAT(TRY_CAST(NULLIF(sl.RecurringStartTime, '') AS DATETIME), 'hh:mm tt') AS RecurringStartTime,
                    sl.RowId,

                    -- Dates formatted as yyyy-MM-dd
                    FORMAT(sl.ScheduleDate, 'yyyy-MM-dd') AS ScheduleDate,
                    FORMAT(sl.ScheduleEndDate, 'yyyy-MM-dd') AS ScheduleEndDate,

                    -- Occurrence Start/End Date with timezone abbreviation

	                 CONCAT(FORMAT(sl.ScheduleDate, 'ddd · dd MMM yyyy'), '. ',CONCAT(FORMAT(COALESCE(TRY_CAST(NULLIF(sl.RecurringStartTime, '') AS DATETIME), '00:00'), 'hh:mm tt'),' ', x.abbreviation)) AS OccuranceStartDate,

					  CONCAT(FORMAT(sl.ScheduleEndDate, 'ddd · dd MMM yyyy'), '. ',CONCAT(FORMAT(COALESCE(TRY_CAST(NULLIF(sl.RecurringEndTime, '') AS DATETIME), '00:00'), 'hh:mm tt'),' ', x.abbreviation)) AS OccuranceEndDate,
                    -- Flag Dates
	                CONVERT(DATETIME, CONVERT(NVARCHAR, sl.ScheduleEndDate, 23) + ' ' + 
	                CONVERT(NVARCHAR, sl.RecurringEndTime, 108), 120) AS OccuranceFlagEndDate,

	                CONVERT(DATETIME, CONVERT(NVARCHAR, sl.ScheduleDate, 23) + ' ' + 
	                CONVERT(NVARCHAR, sl.RecurringStartTime, 108), 120) AS OccuranceFlagStartDate


                FROM Events_Default  ed

                INNER JOIN ProcessInfo ON ed.DocId = ProcessInfo.PrimaryDocId

                INNER JOIN [state] ON [State].StateId = ProcessInfo.CurrentStateId

                INNER JOIN EventRecurringScheduleInterval sl ON ed.docid = sl.eventdocid
                OUTER APPLY
                (
                    SELECT TOP 1 gm_offset, abbreviation
                    FROM Timezone 
                    WHERE time_start <= CAST(DATEDIFF(HOUR,'1970-01-01 00:00:00', sl.ScheduleDate) AS BIGINT) * 60 * 60
                        AND zone_id = ed.Timezone
                    ORDER BY time_start DESC
                ) AS x


                WHERE  ed.Isrecurring = 1 AND [State].StateId IN (21, 22, 56) AND ed.DocId = @EventDocId";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@EventDocId", request.EventDocId);

            if (!string.IsNullOrEmpty(request.Location.Trim()))
            {
                sql = sql + @" AND ed.EventLocation Like '%'+@Filter+'%'";
                queryParameters.Add("@Filter", request.Location);
            }



            if (string.IsNullOrEmpty(request.ScheduleEndDate) && !string.IsNullOrEmpty(request.ScheduleStartDate))
            {

                DateTime startDate = DateTime.Parse(request.ScheduleStartDate);
                var start = startDate.ToString("yyyy-MM-dd 00:00:00:000");
                var end = startDate.ToString("yyyy-MM-dd 23:59:59:000");
                sql = sql + @" AND sl.ScheduleDate <= @EndDate  AND sl.ScheduleEndDate  >= @StartDate";
                queryParameters.Add("@StartDate", start);
                queryParameters.Add("@EndDate", end);
            }

            if (!string.IsNullOrEmpty(request.ScheduleEndDate) && !string.IsNullOrEmpty(request.ScheduleStartDate))
            {

                DateTime startDate = DateTime.Parse(request.ScheduleStartDate);
                DateTime endDate = DateTime.Parse(request.ScheduleEndDate);
                var start = startDate.ToString("yyyy-MM-dd 00:00:00:000");
                var end = endDate.ToString("yyyy-MM-dd 23:59:59:000");

                sql = sql + @" AND sl.ScheduleDate <= @EndDate  AND sl.ScheduleEndDate  >= @StartDate";
                queryParameters.Add("@StartDate", start);
                queryParameters.Add("@EndDate", end);
            }



            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");

            var eventlist = JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(JsonConvert.SerializeObject(result));

            await GetEventImage(eventlist, cancellationToken);

            return eventlist;
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
                evnt["EventLocation"] = string.Join(", ", evnt["County"], evnt["Postcode"], evnt["Country"]);
                evnt.Add("BookingDateList", _mediator.Send(new GetRecurringOccuranceBookingDateListQuery { RowId = Convert.ToInt32(evnt["RowId"]) }).Result);
            }
        }
    }
}
