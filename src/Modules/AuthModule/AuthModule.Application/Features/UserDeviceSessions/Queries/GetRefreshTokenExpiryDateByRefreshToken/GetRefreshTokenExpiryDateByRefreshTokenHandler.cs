using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.UserDeviceSessions.Queries.GetRefreshTokenExpiryDateByRefreshToken
{
    public class GetRefreshTokenExpiryDateByRefreshTokenHandler : IRequestHandler<GetRefreshTokenExpiryDateByRefreshTokenQuery,DateTime?>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private readonly IUtilityService _utilityService;

        public GetRefreshTokenExpiryDateByRefreshTokenHandler(LazyService<IReadRepository<object>> readRepository, IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _utilityService = utilityService;
        }

        public async Task<DateTime?> Handle(GetRefreshTokenExpiryDateByRefreshTokenQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT [RefreshTokenExpiryDate] FROM [dbo].[UserDeviceSessionInfo]
                            WHERE [RefreshToken]=@RefreshToken";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@RefreshToken", request.RefreshToken);
            var result = await _readRepository.Value.GetSingleAsync(sql, cancellationToken, queryParameters, null, "text");
            DateTime? date = null;
            string[] formats = {
                    "M/d/yyyy h:mm:ss tt",   // 1/13/2026 12:57:07 PM
                    "d/M/yyyy h:mm:ss tt",   // 13/1/2026 12:57:07 PM
                    "M/d/yyyy HH:mm:ss",     // 24-hour format without AM/PM
                    "d/M/yyyy HH:mm:ss"      // 24-hour format without AM/PM
                };
            if (result != null && result != DBNull.Value && DateTime.TryParseExact(_utilityService.DecryptData(result.ToString()), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                date = parsedDate;
            }
            return date;
        }
    }
}
