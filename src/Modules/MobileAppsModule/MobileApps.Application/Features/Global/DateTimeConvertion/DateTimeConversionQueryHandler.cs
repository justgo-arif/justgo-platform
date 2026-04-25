using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Application.Features.SystemSetting.Queries.GetGlobalSetting;
using MobileApps.Domain.Entities;

namespace MobileApps.Application.Features.SystemSetting.Queries.GetSystemSettings
{
    class DateTimeConversionQueryHandler : IRequestHandler<DateTimeConversionQuery, string>
    {
        private readonly LazyService<IReadRepository<dynamic>> _readRepository;
        public DateTimeConversionQueryHandler(LazyService<IReadRepository<dynamic>> readRepository)
        {
            _readRepository = readRepository;
         
        }
        public async Task<string> Handle(DateTimeConversionQuery request, CancellationToken cancellationToken)
        {
            string timeData = "";
            string sql = @"select dbo.[GET_UTC_LOCAL_DATE_TIME](@CheckedInAt, @TimeZoneId)";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@CheckedInAt", request.EventDate);
            queryParameters.Add("@TimeZoneId", request.TimeZoneId);

            var result = await _readRepository.Value.GetSingleAsync<dynamic>(sql, queryParameters, null);
          

            if (result!=null)
            {
                timeData= result.ToString("hh:mm tt, dd MMM yyyy");
            }
            return timeData;

        }
    }
}
