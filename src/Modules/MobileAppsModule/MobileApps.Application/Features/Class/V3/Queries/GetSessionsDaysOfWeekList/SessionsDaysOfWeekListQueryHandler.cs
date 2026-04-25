using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V3.Queries.GetSessionsDaysOfWeekList
{
    class SessionsDaysOfWeekListQueryHandler : IRequestHandler<SessionsDaysOfWeekListQuery, IEnumerable<object>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public SessionsDaysOfWeekListQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<IEnumerable<object>> Handle(SessionsDaysOfWeekListQuery request, CancellationToken cancellationToken)
        {
            string sql = @";WITH DaysOfWeek AS (SELECT v.DayOfWeek, v.SortOrder FROM (VALUES('Fri', 5),('Sat', 6),('Sun', 7),('Mon', 1),('Tue', 2),('Wed', 3),('Thu', 4)) v(DayOfWeek, SortOrder))
            SELECT 
                d.DayOfWeek,
                d.SortOrder,
                CASE 
                    WHEN t.DayOfWeek IS NULL THEN 0 
                    ELSE 1 
                END AS IsActive
            FROM DaysOfWeek d
            LEFT JOIN (
             select  ss.DayOfWeek from JustGoBookingClassSession cs
             inner join JustGoBookingClassSessionSchedule ss on cs.SessionId=ss.SessionId
             inner join JustGoBookingClass bc on cs.ClassId=bc.ClassId
             where bc.OwningEntityId=@ClubDocId AND ss.IsDeleted<>1
             group by  ss.DayOfWeek

            ) t ON d.DayOfWeek = t.DayOfWeek
            ORDER BY d.SortOrder";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ClubDocId", request.ClubDocId);

            return await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");
        }
    }
}
