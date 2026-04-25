using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Newtonsoft.Json;

namespace AuthModule.Application.Features.Users.Queries.GetUserByLoginId
{
    public class GetUserByLoginIdHandler:IRequestHandler<GetUserByLoginIdQuery,User>
    {
        private readonly LazyService<IReadRepository<User>> _readRepository;
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public GetUserByLoginIdHandler(LazyService<IReadRepository<User>> readRepository, IMediator mediator
            , ISystemSettingsService systemSettingsService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }

        public async Task<User> Handle(GetUserByLoginIdQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT *
                          FROM [dbo].[User]
                          WHERE [LoginId]=@LoginId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@LoginId", request.LoginId);
            var result = await _readRepository.Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");

            var user = JsonConvert.DeserializeObject<User>(JsonConvert.SerializeObject(result));

               if(user!=null) await GetUserImage(user,cancellationToken);
            return user;
        }
        private async Task GetUserImage(User user, CancellationToken cancellationToken)
        {
            HttpClient _httpClient = new HttpClient();
            string storeRoot = await _systemSettingsService.GetSystemSettings("SYSTEM.AZURESTOREROOT" ,cancellationToken);
            string hostMid = await _systemSettingsService.GetSystemSettings("CLUBPLUS.HOSTSYSTEMID", cancellationToken);
            string siteUrl = await _systemSettingsService.GetSystemSettings("SYSTEM.SITEADDRESS", cancellationToken);
            string baseUrl = "";
            string url = "";
            try
            {
               
                HttpResponseMessage response = null;
                if (!string.IsNullOrEmpty(user.ProfilePicURL))
                {
                    baseUrl = storeRoot + "/002/" + hostMid;
                    url = baseUrl + "/User/" + user.Userid + "/" + user.ProfilePicURL;
                    response = await _httpClient.GetAsync(url);
                }
                if (string.IsNullOrEmpty(user.ProfilePicURL) || !response.IsSuccessStatusCode)
                {
                    url = siteUrl + "/Media/Images/";
                    string img = "avatar-" + user.Gender + ".png";
                    url = url + img;
                }
            }
            catch 
            {
            }
            

            user.ProfilePicURL = url;
        }
    }
}
