using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities.MFA;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;

namespace AuthModule.Application.Features.MFA.Queries.GetTimeZoneValue
{
    public class IsEnableMFAQueryHandler:IRequestHandler<IsEnableMFAQuery, MFACommonResponse>
    {
        private readonly LazyService<IReadRepository<MFACommonResponse>> _readRepository;

        public IsEnableMFAQueryHandler(LazyService<IReadRepository<MFACommonResponse>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<MFACommonResponse> Handle(IsEnableMFAQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT 
                            CAST(ISNULL((SELECT TOP 1
                                            CASE
                                                WHEN DATEDIFF(DAY, RememberEnableDate, GETDATE()) > 30 THEN 0
                                                ELSE IsRememberEnabled
                                            END
                                        FROM [dbo].[MFARememberedDevices]
                                        WHERE UserID = u.UserID AND DeviceIdentifier = @DeviceIdentifier
                                        ORDER BY RememberEnableDate DESC), 
                                    0) AS BIT
                                ) AS IsDeviceRemembered,
                                mfa.EnableAuthenticatorApp,
                                mfa.EnableWhatsapp,
                                mfa.EnableWhatsappDate,
                                mfa.EnableAuthenticatorAppDate,
                                RIGHT(mfa.WhatsAppNumber, 4) AS WhatsAppNumber,
                                mfa.IsEmailAuthEnabled,
                                mfa.Email
                            FROM [userMFA] mfa INNER JOIN [User] u on u.Userid=mfa.userid
                    WHERE u.LoginId = @UserName";


            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserName", request.UserName);
            queryParameters.Add("@DeviceIdentifier", request.DeviceIdentifier);

            var result = await _readRepository.Value.GetAsync(sql, queryParameters, null, "text");

            return result;
        }
    }
}
