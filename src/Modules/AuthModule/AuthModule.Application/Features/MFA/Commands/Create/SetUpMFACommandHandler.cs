using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities.MFA;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.MFA.Commands.Create
{
    public class SetUpMFACommandHandler:IRequestHandler<SetUpMFACommand,bool>
    {
        private readonly LazyService<IWriteRepository<UserMFA>> _writeRepository;
        private readonly IUtilityService _utilityService;
        public SetUpMFACommandHandler(LazyService<IWriteRepository<UserMFA>> writeRepository, IUtilityService utilityService)
        {
            _writeRepository = writeRepository;
            _utilityService = utilityService;
        }

        public async Task<bool> Handle(SetUpMFACommand request, CancellationToken cancellationToken)
        {
            string sql = "";
            switch (request.AuthChannel.ToLower())
            {
                case "whatsapp":
                    sql = @"if not exists(select top 1 UserId from UserMFA where UserId = @UserId)
                            begin
						    	insert into UserMFA (UserId,WhatsAppNumber,EnableWhatsappDate,PhoneCode,CountryCode)
						    	values (@UserId,@WhatsAppNumber,GETDATE(),@PhoneCode,@CountryCode)
                            end
                            else
                            begin
                            	update UserMFA set WhatsAppNumber = @WhatsAppNumber,EnableWhatsappDate=GETDATE(),PhoneCode = @PhoneCode,CountryCode = @CountryCode where UserId = @UserId
                            end";
                    break; 
                case "authapp":
                    sql = @"if not exists(select top 1 UserId from UserMFA where UserId = @UserId)
                            begin
						    	insert into UserMFA (UserId,AuthenticatorKey,EnableAuthenticatorAppDate)
						    	values (@UserId,@AuthenticatorKey,GETUTCDATE())
                            end
                            else
                            begin
                            	update UserMFA set AuthenticatorKey = @AuthenticatorKey,EnableAuthenticatorAppDate =GETUTCDATE() where UserId = @UserId
                            end";
                    break;
                case "emailauth":
                    sql = @"IF NOT EXISTS (SELECT TOP 1 UserId FROM UserMFA WHERE UserId = @UserId)
	                            BEGIN
		                            INSERT INTO UserMFA (UserId, Email, EmailAuthEnableDate)
		                            VALUES (@UserId, @Email, GETUTCDATE())
	                            END
                            ELSE
	                            BEGIN
		                            UPDATE UserMFA SET Email = @Email, EmailAuthEnableDate = GETUTCDATE()
		                            WHERE UserID = @UserId
	                            END";
                    break;
                default:
                    break;
            }

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", request.UserId);
            queryParameters.Add("@AuthenticatorKey", request.UserMFA.AuthenticatorKey == null || request.UserMFA.AuthenticatorKey == "" ? "" : _utilityService.EncryptData(request.UserMFA.AuthenticatorKey));
            queryParameters.Add("@WhatsAppNumber", request.UserMFA.WhatsAppNumber.Trim());
            queryParameters.Add("@PhoneCode", request.UserMFA.PhoneCode);
            queryParameters.Add("@CountryCode", request.UserMFA.CountryCode.Trim());
            queryParameters.Add("@Email", request.UserMFA.Email.Trim());


            var result = await _writeRepository.Value.ExecuteAsync(sql, queryParameters, null, "text");

            return result >= 0;
        }
    }
}
