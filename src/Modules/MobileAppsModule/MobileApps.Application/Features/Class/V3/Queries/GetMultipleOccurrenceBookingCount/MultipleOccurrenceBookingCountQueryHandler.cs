using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;

namespace MobileApps.Application.Features.Class.V3.Queries.GetMultipleOccurrenceBookingCount
{
    public class MultipleOccurrenceBookingCountQueryHandler:IRequestHandler<MultipleOccurrenceBookingCountQuery,IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public MultipleOccurrenceBookingCountQueryHandler(LazyService<IReadRepository<object>> readRepository)
        {
            _readRepository = readRepository;

        }
        public async Task<IList<IDictionary<string, object>>> Handle(MultipleOccurrenceBookingCountQuery request, CancellationToken cancellationToken)
        {

            string sql = ClassMultipleOccurrenceAttendeeCountListSql();
            string sqlWhere = " ad.OccurenceId IN @OccurrenceId AND (ad.AttendeeDetailsStatus IS NULL OR ad.AttendeeDetailsStatus=1)  AND (cs.IsDeleted<>1 AND ss.IsDeleted<>1) ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@OccurrenceId", request.OccurrenceIds);

            sql = sql.Replace("@sqlWhere", sqlWhere);

            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");


            return AttendanceCountShared.MergeClassStatusLists(result) ?? new List<IDictionary<string, object>>();
        }
        private string ClassMultipleOccurrenceAttendeeCountListSql()
        {
            return @"select a.Status as BookingTypeId,
            CASE 
            WHEN a.[Status] = 1 THEN 'One-off'
            WHEN a.[Status] = 2 THEN 'Trial'
            WHEN a.[Status] = 3 THEN 'Payg'
            ELSE 'Subscription'
            END AS BookingType,
            CASE 
            WHEN ad.[Status] IS NULL OR ad.[Status] = '' THEN 'Pending' 
            ELSE ad.[Status] 
            END AS StatusName
            ,COUNT(a.AttendeeId) AS StatusCount
            -- TrialCount: Count only attendees where a.Status = 2
            ,SUM(CASE WHEN a.Status = 2 THEN 1 ELSE 0 END) AS TrialCount

            from JustGoBookingAttendee a
                inner join JustGoBookingClassSession cs on a.SessionId=cs.SessionId
                inner join JustGoBookingClassSessionSchedule ss on cs.SessionId=ss.SessionId
                inner join  JustGoBookingScheduleOccurrence so on ss.SessionScheduleId=so.ScheduleId
              
                inner join JustGoBookingAttendeeDetails ad on so.OccurrenceId=ad.OccurenceId AND a.AttendeeId=ad.AttendeeId
                left join JustGoBookingAttendeeDetailNote dn on ad.AttendeeDetailsId=dn.AttendeeDetailsId

                left join JustGoBookingAttendeePayment ap on ad.AttendeePaymentId=ap.AttendeePaymentId
                left join JustGoBookingClassSessionProduct sp on ap.ProductId=sp.ProductId 
            where @sqlWhere
            GROUP BY a.Status,ad.[Status];";

        }

    }
}
