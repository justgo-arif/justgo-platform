using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace MobileApps.Application.Features.Class.V2.Queries.GetOccurrenceBookingCount
{
    class ClassBookingCountQueryHandler : IRequestHandler<ClassBookingCountQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public ClassBookingCountQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;

        }
        public async Task<IList<IDictionary<string, object>>> Handle(ClassBookingCountQuery request, CancellationToken cancellationToken)
        {
            string sql = ClassOccurrenceAttendeeListSql();
            string sqlWhere = " c.OwningEntitySyncGuid=@ClubGuid  AND (ad.AttendeeDetailsStatus IS NULL OR ad.AttendeeDetailsStatus=1) AND  CAST(so.StartDate AS DATE) =CAST(@StartDate AS DATE)";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ClubGuid", request.ClubSyncGuid);
            queryParameters.Add("@StartDate", DateTime.UtcNow);



            sql = sql.Replace("@sqlWhere", sqlWhere);

            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");


            return AttendanceCountShared.MergeClassStatusLists(result);
        }

        private string ClassOccurrenceAttendeeListSql()
        {
            return @"SELECT 
                    a.Status as BookingTypeId,
                    CASE 
                    WHEN a.[Status] = 1 THEN 'One-off'
                    WHEN a.[Status] = 2 THEN 'Trial'
                    WHEN a.[Status] = 3 THEN 'Payg'
                    ELSE 'Subscription'
                    END AS BookingType,
                    CASE 
                    WHEN ad.[Status] IS NULL OR ad.[Status] = '' THEN 'Pending' 
                    ELSE ad.[Status] 
                    END AS StatusName,
                    COUNT(a.AttendeeId) AS StatusCount,
                    -- TrialCount: Count only attendees where a.Status = 2
                    SUM(CASE WHEN a.Status = 2 THEN 1 ELSE 0 END) AS TrialCount

                    from JustGoBookingClass c
                inner join JustGoBookingClassSession cs on c.ClassId=cs.ClassId
					inner Join JustGoBookingAttendee a on cs.SessionId=a.SessionId
                inner join JustGoBookingClassSessionSchedule ss on cs.SessionId=ss.SessionId
                inner join  JustGoBookingScheduleOccurrence so on ss.SessionScheduleId=so.ScheduleId
                inner join JustGoBookingAttendeePayment ap on a.AttendeeId=ap.AttendeeId
                left join JustGoBookingAttendeeDetails ad on ad.OccurenceId=so.OccurrenceId AND a.AttendeeId=ad.AttendeeId
                left join JustGoBookingAttendeeDetailNote dn on ad.AttendeeDetailsId=dn.AttendeeDetailsId

                    where @sqlWhere
                    GROUP BY a.Status,ad.[Status];";

        }

    }
}
