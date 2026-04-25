using System.Threading;
using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Application.Features.SystemSetting.Queries;
using MobileApps.Application.Features.SystemSetting.Queries.GetSystemSettings;
using MobileApps.Domain.Entities;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Members.Queries.GetMemberByMemberDocId
{
    class GetUserByUserIdQueryHandler : IRequestHandler<GetUserByUserIdQuery, UserViewModel>
    {
        private readonly LazyService<IReadRepository<UserViewModel>> _readRepository;
        private  IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public GetUserByUserIdQueryHandler(LazyService<IReadRepository<UserViewModel>> readRepository, IMediator mediator
            , ISystemSettingsService systemSettingsService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }

        public async Task<UserViewModel> Handle(GetUserByUserIdQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT Userid,MemberId,MemberDocId,ProfilePicURL,Gender,EmailAddress,
            Contact=(select top 1 p.Number from UserPhoneNumber p where p.[Type]='Mobile' AND p.UserId=Userid)
            FROM [dbo].[User]
            WHERE MemberDocId=@MemberDocId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@MemberDocId", request.MemberDocId);
            var result = await _readRepository.Value.GetAsync(sql, queryParameters, null, "text");
            //var member = JsonConvert.DeserializeObject<UserViewModel>(JsonConvert.SerializeObject(result));

            if(result!=null) await GetMemberImage(result,cancellationToken);

            return result;
        }
        private async Task GetMemberImage(UserViewModel member, CancellationToken cancellationToken)
        {
            HttpClient _httpClient = new HttpClient();
            var itemKeys = "SYSTEM.AZURESTOREROOT,CLUBPLUS.HOSTSYSTEMID,SYSTEM.SITEADDRESS";
            var systemSettings = await _systemSettingsService.GetSystemSettingsByMultipleItemKey(itemKeys, cancellationToken);
            var storeRoot = systemSettings?.Where(w => w.ItemKey == "SYSTEM.AZURESTOREROOT")?.Select(s => s.Value).SingleOrDefault();
            var hostMid = systemSettings?.Where(w => w.ItemKey == "CLUBPLUS.HOSTSYSTEMID")?.Select(s => s.Value).SingleOrDefault();
            var siteUrl = systemSettings?.Where(w => w.ItemKey == "SYSTEM.SITEADDRESS")?.Select(s => s.Value).SingleOrDefault();
            HttpResponseMessage response = null;
            string baseUrl = "";
            string url = "";
            try
            {
                if (!string.IsNullOrEmpty(member.ProfilePicURL))
                {
                    baseUrl = storeRoot + "/002/" + hostMid;
                    url = baseUrl + "/User/" + member.Userid + "/" + member.ProfilePicURL;
                    response = await _httpClient.GetAsync(url);
                }
                if (string.IsNullOrEmpty(member.ProfilePicURL) || !response.IsSuccessStatusCode)
                {
                    url = siteUrl + "/Media/Images/";
                    string img = "avatar-" + member.Gender + ".png";
                    url = url + img;
                }
                member.ProfilePicURL = url;
            }
            catch
            {

            }
           
        }

    }
}
