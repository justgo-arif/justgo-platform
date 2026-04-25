using AuthModule.Domain.Entities.MFA;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.MFA.Queries.GetTimeZoneValue;

public class GetTimeZoneValueQueryHandler : IRequestHandler<GetTimeZoneValueQuery, TimeZoneMFA>
{
    private readonly LazyService<IReadRepository<TimeZoneMFA>> _readRepository;

    public GetTimeZoneValueQueryHandler(LazyService<IReadRepository<TimeZoneMFA>> readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<TimeZoneMFA> Handle(GetTimeZoneValueQuery request, CancellationToken cancellationToken)
    {
        string sql = @"declare @TimeZone int = (select [value] from SystemSettings where ItemKey  = 'ORGANISATION.TIMEZONE')

						   select top  1 gm_offset,abbreviation as TimeZone,DATEADD(SECOND,gm_offset,getdate()) Date  from Timezone where time_start <=  cast(DATEDIFF(HOUR,'1970-01-01 00:00:00', getdate()) as bigint)*60*60
                          and zone_id=@TimeZone order by time_start desc";

        return await _readRepository.Value.GetAsync(sql, null, null, "text");
    }
}
