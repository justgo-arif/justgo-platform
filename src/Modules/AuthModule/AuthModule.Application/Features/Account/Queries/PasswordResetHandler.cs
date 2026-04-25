using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using AuthModule.Application.Features.Tenants.Queries.GetTenantByTenantClientId;
using AuthModule.Application.Features.Users.Queries.GetUserByLoginId;
using AuthModule.Domain.Entities;
using Dapper;
using JustGoAPI.Shared.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using AuthModule.Application.EmailServices;
using Microsoft.IdentityModel.Tokens;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;

namespace AuthModule.Application.Features.Account.Queries
{
    public class PasswordResetHandler : IRequestHandler<PasswordResetQuery, Tuple<bool, string>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private readonly LazyService<IWriteRepository<object>> _writeRepository;
        private IMediator _mediator;
        private readonly LazyService<EmailService> _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISystemSettingsService _systemSettingsService;
        private readonly IUtilityService _utilityService;
        public PasswordResetHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator
            , LazyService<IWriteRepository<object>> writeRepository, LazyService<EmailService> emailService
            , IHttpContextAccessor httpContextAccessor, ISystemSettingsService systemSettingsService
            , IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _writeRepository = writeRepository;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _systemSettingsService = systemSettingsService;
            _utilityService = utilityService;
        }

        public async Task<Tuple<bool,string>> Handle(PasswordResetQuery request, CancellationToken cancellationToken)
        {
            string responseMessage = "";
            bool isSuccess = false;
            
            try
            {
                if (string.IsNullOrWhiteSpace(request.OrganizationId))
                {
                    request.OrganizationId = await _utilityService.GetTenantClientIdByDomain(cancellationToken);
                }
                _httpContextAccessor.HttpContext.Items["tenantClientId"] = request.OrganizationId;
                var tenant = await _mediator.Send(new GetTenantByTenantClientIdQuery(request.OrganizationId));

                if (tenant == null) { 
                    responseMessage = "Organization isn't valid";
                    return Tuple.Create(isSuccess, responseMessage);
                }


                var user = await _mediator.Send(new GetUserByLoginIdQuery(request.UserId));
                if (user == null) {
                    responseMessage = "User isn't valid";
                    return Tuple.Create(isSuccess, responseMessage);
                } 

                var resetHash = GenerateResetToken();

                var siteAddress = await GetSiteUrl(cancellationToken);
                var url = new Uri(string.Format("{0}Account.mvc/ResetPassword?{1}",
                    siteAddress,
                    resetHash
                    ));


                var queryParameters = new DynamicParameters();
                queryParameters.Add("@Name", user.FirstName);
                queryParameters.Add("@EmailAddress", user.EmailAddress);
                queryParameters.Add("@ResetURL", url.ToString());
                queryParameters.Add("@Token", resetHash);
                queryParameters.Add("@UserId", user.Userid);
                await _writeRepository.Value.ExecuteAsync("SendResetPasswordEmail", cancellationToken, queryParameters,null);
                //execute sp SendResetPasswordEmail end


                //update Members_Default start
                string sqlDefaultMember = @"update  Members_Default set PasswordResetToken = @Token where DocId=(select MemberDocId from [User] where UserId =@UserId)";

                var queryParametersMember = new DynamicParameters();
                queryParametersMember.Add("@Token", resetHash);
                queryParametersMember.Add("@UserId", user.Userid);


                await _writeRepository.Value.ExecuteAsync(sqlDefaultMember, cancellationToken, queryParametersMember, null, "text");
                //update Members_Default end


                //execute sp SEND_EMAIL_BY_SCHEME start

                var queryParametersSendEmail = new DynamicParameters();
                queryParametersSendEmail.Add("@ForEntityId", user.Userid);
                queryParametersSendEmail.Add("@MessageScheme", "Account\\Password Reset(User)");
                queryParametersSendEmail.Add("@GetInfo", 0);
                queryParametersSendEmail.Add("@OwnerType", "NGB");
                queryParametersSendEmail.Add("@OwnerId", 0);
                queryParametersSendEmail.Add("@Argument", url.ToString());

                    
                await _writeRepository.Value.ExecuteAsync("SEND_EMAIL_BY_SCHEME", cancellationToken, queryParametersSendEmail);
                //execute sp SEND_EMAIL_BY_SCHEME end
                await _emailService.Value.Execute();

                int atIndex = user.EmailAddress.IndexOf('@');

                // Get the first two characters before the '@'
                string firstThreeChars = user.EmailAddress.Substring(0, 3);

                // Get the part after the '@'
                string afterAt = user.EmailAddress.Substring(atIndex + 1);


                responseMessage = "Thanks, we have sent an email to the matching email:"+ firstThreeChars + "***@"+ afterAt+ ", please check this and follow the link.";
                isSuccess = true;
               


            }
            catch (Exception ex)
            {
                responseMessage = "Sorry, we can't send an email to any matching username, please check this and follow the link."+ ex.Message;
                isSuccess = false;
            }
            return Tuple.Create(isSuccess,responseMessage);
        }

        private string GenerateResetToken()
        {
            // Generate a random number of bytes
            byte[] tokenBytes = new byte[32]; // Example byte array length

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }

            // Convert to base64 string
            string resetToken = Convert.ToBase64String(tokenBytes);

            resetToken = Guid.NewGuid().ToString() + Guid.NewGuid().ToString();

            // Optionally, you can hash the token using SHA256 for further security
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(resetToken));
                StringBuilder hashString = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    hashString.Append(b.ToString("X2"));
                }
                return hashString.ToString();
            }
        }
        private async Task<string> GetSiteUrl(CancellationToken cancellationToken)
        {
            var appPath = await _systemSettingsService.GetSystemSettings("SYSTEM.SITEADDRESS", cancellationToken);

            //appPath += !appPath.IsNullOrEmpty() && (appPath.LastIndexOf("/", StringComparison.Ordinal) == appPath.Length - 1) ? "" : "/";
            appPath += !string.IsNullOrEmpty(appPath) && (appPath.LastIndexOf("/", StringComparison.Ordinal) == appPath.Length - 1) ? "" : "/";

            return appPath;
        }
    }
}
