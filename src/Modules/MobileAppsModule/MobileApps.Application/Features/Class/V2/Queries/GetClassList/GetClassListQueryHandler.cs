using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Application.Features.SystemSetting.Queries;
using MobileApps.Application.Features.SystemSetting.Queries.GetSystemSettings;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MobileApps.Application.Features.Class.V2.Queries.GetClassBookingList
{
    class GetClassListQueryHandler : IRequestHandler<GetClassListQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public GetClassListQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator
            , ISystemSettingsService systemSettingsService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(GetClassListQuery request, CancellationToken cancellationToken)
        {

            string sql = @"SELECT distinct
                c.ClassId,c.[Name],c.StateId, att.EntityTypeId,c.ClassBookingType,
                att.[Name] AS [Location],
                att.[Path] AS StorePath, 
                (
                    SELECT Count(ss.SessionId)
                    FROM JustGoBookingClassSession ss
                    JOIN JustGoBookingClassTerm tm 
                        ON ss.TermId = tm.ClassTermId
			            AND ss.ClassId = c.ClassId
                    --FOR JSON PATH
                ) AS SessionCount
            FROM JustGoBookingClass c
            left join JustGoBookingClassSession cs on c.ClassId=cs.ClassId
            inner join  JustGoBookingClassSessionSchedule ss on cs.SessionId=ss.SessionId
            inner join JustGoBookingScheduleOccurrence o on ss.SessionScheduleId=o.ScheduleId
            LEFT JOIN JustGoBookingAttachment att ON att.EntityId = c.ClassId
            WHERE @sqlWhere;";

            string sqlWhere = "c.OwningEntitySyncGuid=@ClubSyncGuid AND (c.IsDeleted = 0 OR c.IsDeleted IS NULL) AND c.StateId in(2,3)";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ClubSyncGuid", request.ClubSyncGuid);

            if (!string.IsNullOrEmpty(request.ClassName.Trim()))
            {
                sqlWhere = sqlWhere + @" AND c.[Name] Like '%'+@Filter+'%'";
                queryParameters.Add("@Filter", request.ClassName);
            }

            if (!request.EndDate.HasValue && request.StartDate.HasValue)
            {

                DateTime startDate = DateTime.Parse(request.StartDate.ToString());
                var start = startDate.ToString("yyyy-MM-dd 00:00:00");
                var end = startDate.ToString("yyyy-MM-dd 23:59:59");

                sqlWhere = sqlWhere + @" AND o.StartDate <= @EndDate AND o.EndDate>= @StartDate";
                queryParameters.Add("@StartDate", DateTime.Parse(start));
                queryParameters.Add("@EndDate", DateTime.Parse(end));
            }
            else if (request.EndDate.HasValue && request.StartDate.HasValue)
            {

                DateTime startDate = DateTime.Parse(request.StartDate.ToString());
                DateTime endDate = DateTime.Parse(request.EndDate.ToString());

                var start = startDate.ToString("yyyy-MM-dd 00:00:00");
                var end = endDate.ToString("yyyy-MM-dd 23:59:59");
                sqlWhere = sqlWhere + @" AND o.StartDate <= @EndDate AND o.EndDate>= @StartDate";
                queryParameters.Add("@StartDate", DateTime.Parse(start));
                queryParameters.Add("@EndDate", DateTime.Parse(end));
            }
            sql = sql.Replace("@sqlWhere", sqlWhere);

            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");

            var classList = JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(JsonConvert.SerializeObject(result));

            if (classList?.Count > 0) { 
                await GetEventImage(classList, cancellationToken);
            }

            return classList;

        }

        private async Task GetEventImage(IList<IDictionary<string, object>> classList, CancellationToken cancellationToken)
        {
            var itemKeys = "SYSTEM.AZURESTOREROOT,CLUBPLUS.HOSTSYSTEMID,SYSTEM.SITEADDRESS,EVENT.DEFAULT_IMAGE";
            var systemSettings = await _systemSettingsService.GetSystemSettingsByMultipleItemKey(itemKeys, cancellationToken);
            var storeRoot = systemSettings?.Where(w => w.ItemKey == "SYSTEM.AZURESTOREROOT")?.Select(s => s.Value).SingleOrDefault();
            var hostMid = systemSettings?.Where(w => w.ItemKey == "CLUBPLUS.HOSTSYSTEMID")?.Select(s => s.Value).SingleOrDefault();
            var siteUrl = systemSettings?.Where(w => w.ItemKey == "SYSTEM.SITEADDRESS")?.Select(s => s.Value).SingleOrDefault();
            var eventDefaultImg = systemSettings?.Where(w => w.ItemKey == "EVENT.DEFAULT_IMAGE")?.Select(s => s.Value).SingleOrDefault();

            HttpClient _httpClient = new HttpClient();

            foreach (var cls in classList)
            {
                HttpResponseMessage response = null;
                string baseUrl = "";
                string url = "";
                try
                {

                    if (cls["Location"]!=null)
                    {
                        baseUrl = storeRoot + "/002/" + hostMid;
                        url = baseUrl + "/justgobookingattachment/" + cls["EntityTypeId"] + "/" + cls["ClassId"] + "/" + cls["Location"].ToString();
                        response = await _httpClient.GetAsync(url);
                    }
                    if (response!=null &&!response.IsSuccessStatusCode)
                    {
                        url = siteUrl + cls["StorePath"] + cls["Location"];
                    }
                    cls["Location"] = url;
                }
                catch { }
               
                //remove 2 keys
                cls.Remove("StorePath");
                cls.Remove("EntityTypeId");
            }

        }
    }
}
