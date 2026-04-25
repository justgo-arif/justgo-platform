using System.Threading;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Application.Features.SystemSetting.Queries;
using MobileApps.Application.Features.SystemSetting.Queries.GetSystemSettings;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Event.V2.Queries.GetEventByClub
{
    class GetEventByClubListQueryHandler : IRequestHandler<GetEventByClubListQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public GetEventByClubListQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator
            , ISystemSettingsService systemSettingsService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(GetEventByClubListQuery request, CancellationToken cancellationToken)
        {
            string sql = @"select  * from events_Default 
                inner join
                ProcessInfo on events_Default.DocId = ProcessInfo.PrimaryDocId
                inner join
                [state] on [State].StateId = ProcessInfo.CurrentStateId  where [State].StateId in (21,22,56) AND OwningEntityid=@ClubDocId";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ClubDocId", request.ClubDocId);

            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");
            var eventList = JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(JsonConvert.SerializeObject(result));

            await GetEventImage(eventList,cancellationToken);

            return eventList;
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
                    if (evnt["Location"].ToString().Equals("virtual", StringComparison.CurrentCultureIgnoreCase) || !response.IsSuccessStatusCode)
                    {
                        url = siteUrl + "/media/images/organization/EventDefaultImage/";
                        url = url + eventDefaultImg;
                    }
                   
                }
                catch { }

                evnt["Location"] = url;
            }
        }
    }
}
