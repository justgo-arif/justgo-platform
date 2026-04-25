using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V3.Queries.GetClassBookingList
{
    class GetClassListQueryHandler : IRequestHandler<GetClassListQuery, IList<IDictionary<string,object>>>
    {
        private readonly LazyService<IReadRepository<dynamic>> _readRepository;
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public GetClassListQueryHandler(LazyService<IReadRepository<dynamic>> readRepository, IMediator mediator
            , ISystemSettingsService systemSettingsService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(GetClassListQuery request, CancellationToken cancellationToken)
        {

            string sqlWhere = "c.OwningEntitySyncGuid=@ClubGuid AND ISNULL(c.IsDeleted, 0) = 0 AND c.StateId in(2,3) AND ISNULL(cs.IsDeleted, 0) = 0";
            string categoryWhere = @"";
            string coachWhere = @"";
            string productWhere = @"";
            string filterJoinQuery = @"";
            string sql = WhereConditionBuilder(request);

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ClubGuid", request.ClubGuid);

            // Class Name filter
            if (!string.IsNullOrWhiteSpace(request.ClassName))
            {
                sqlWhere += "\n  AND cs.[Name]  Like '%'+@Filter+'%'";
                queryParameters.Add("@Filter", request.ClassName.Trim());
            }

            // Date Range filter
            if (request.StartDate.HasValue && !request.EndDate.HasValue)
            {
                var start = request.StartDate?.Date ?? DateTime.MinValue;

                sqlWhere += "\n  AND CAST(o.StartDate as Date) = CAST(@StartDate as Date)";
                queryParameters.Add("@StartDate", start.ToString("yyyy-MM-dd"));

            }
            //extend  END DATE FILTERING
            if (request.StartDate.HasValue && request.EndDate.HasValue)
            {
                var start = request.StartDate?.Date ?? DateTime.MinValue;
                var end = request.EndDate?.Date ?? DateTime.MaxValue;

                sqlWhere += "\n AND CAST(o.EndDate AS DATE) >= CAST(@StartDate AS DATE) AND CAST(o.StartDate AS DATE) <= CAST(@EndDate AS DATE)";
                queryParameters.Add("@StartDate", start.ToString("yyyy-MM-dd"));
                queryParameters.Add("@EndDate", end.ToString("yyyy-MM-dd"));

            }
            // Time Filter
            if (!string.IsNullOrEmpty(request.TimeFilter))
            {
                sqlWhere += "\n  AND CAST(ss.StartTime AS TIME) = CAST(@TimeFilter AS TIME)";
                queryParameters.Add("@TimeFilter", request.TimeFilter);
            }
      
            // Age Groups
            if (request.AgeGroupIds?.Any() == true)
            {
                sqlWhere += "\n  AND ag.Id IN @AgeGroupIds";
                queryParameters.Add("@AgeGroupIds", request.AgeGroupIds);
            }

            // Color Groups
            if (request.ColorGroupIds?.Any() == true)
            {
                sqlWhere += "\n  AND cg.ColorGroupId IN @ColorGroupIds";
                queryParameters.Add("@ColorGroupIds", request.ColorGroupIds);
            }
            // Genders
            if (request.Genders?.Any() == true)
            {
                sqlWhere += "\n  AND EXISTS (SELECT 1 FROM STRING_SPLIT(op.Gender, ',') AS s WHERE TRIM(s.value) IN @Genders)";
                queryParameters.Add("@Genders", request.Genders);
            }
            // Categories
            if (request.CategoryIds?.Any() == true)
            {
                categoryWhere += "WHERE ca.CategoryId IN @CategoryIds";
                filterJoinQuery = filterJoinQuery + "\n INNER JOIN CategoryData cd on cd.ClassId=c.ClassId\n";
                queryParameters.Add("@CategoryIds", request.CategoryIds);
            }
            // Coaches
            if (request.CoachIds?.Any() == true)
            {
                coachWhere += "WHERE ISNULL(jbc.IsDeleted,0)=0 AND  jbc.BookingContactId IN @CoachIds";
                filterJoinQuery = filterJoinQuery + "\n INNER JOIN CoachData jcd on jcd.SessionId = cs.SessionId  \n";
                queryParameters.Add("@CoachIds", request.CoachIds);
            }
            // Product Types
            if (request.ProductTypeIds?.Any() == true)
            {
                productWhere += "WHERE ISNULL(spro.ProductType,0)>0 AND ISNULL(spro.ProductType,0) IN @ProductTypeIds";
                filterJoinQuery = filterJoinQuery + "\n INNER JOIN  ProductData pd on cs.SessionId=pd.SessionId \n";
                queryParameters.Add("@ProductTypeIds", request.ProductTypeIds);
            }


            sql = sql.Replace("@sqlWhere", sqlWhere);
            sql = sql.Replace("@categoryWhere", categoryWhere.Length > 0 ? categoryWhere : "\n--\n");
            sql = sql.Replace("@coachWhere", coachWhere.Length > 0 ? coachWhere : "\n--\n");
            sql = sql.Replace("@productWhere", productWhere.Length > 0 ? productWhere : "\n--\n");
            sql = sql.Replace("@filterSqlJoin", filterJoinQuery.Length > 0 ? filterJoinQuery : "\n--\n");
            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");



            var dataList = result.Select(m =>
            {
                m.GenderList = string.IsNullOrWhiteSpace(m.GenderList)
                    ? new List<string>()
                    : JsonConvert.DeserializeObject<List<string>>(m.GenderList);

                m.AttendeeCountList = string.IsNullOrWhiteSpace(m.AttendeeCountList)
                    ? new List<dynamic>()
                    : JsonConvert.DeserializeObject<List<dynamic>>(m.AttendeeCountList);

                return m;
            }).ToList();

            return JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(JsonConvert.SerializeObject(dataList)) ?? new List<IDictionary<string, object>>();

        }


        private string WhereConditionBuilder(GetClassListQuery request)
        {
            int nextId = request.NextId <= 0 ? 0 : request.NextId + 1;
            int dataSize = (request.NextId + request.DataSize);
            return $@";WITH DaysOfWeek AS ( 
                SELECT v.DayOfWeek, v.FullDayName
                FROM (VALUES
                    ('Mon', 'Monday'),
                    ('Tue', 'Tuesday'),
                    ('Wed', 'Wednesday'),
                    ('Thu', 'Thursday'),
                    ('Fri', 'Friday'),
                    ('Sat', 'Saturday'),
                    ('Sun', 'Sunday')
                ) v(DayOfWeek, FullDayName)
            ),
            CategoryData as (
	             select  distinct cl.ClassId from JustGoBookingClass cl
	            inner join JustGoBookingCategory ca on ca.OwnerId=cl.OwningEntityId 
                @categoryWhere
             ),
             CoachData as (
	            select  distinct jbc.EntityId as SessionId from  JustGoBookingContact jbc 
                @coachWhere
	         
             ),
             ProductData as (
	              SELECT distinct spro.SessionId
				FROM JustGoBookingClassSessionProduct spro
				@productWhere
				GROUP BY spro.SessionId,spro.ProductType
             ),DistinctSessionOption AS (
				-- Remove exact duplicates
				SELECT DISTINCT SessionId, Gender
				FROM JustGoBookingClassSessionOption
			),
            CTEAllData AS (
                SELECT 
                    c.ClassId,
                    cs.SessionId,
                    o.OccurrenceId,
                    c.[Name] AS ClassGroup,
                    cs.[Name] AS ClassName,
                    CONCAT(FORMAT(CAST(ss.StartTime AS datetime),'hh:mm tt'),',', d.FullDayName,'.',FORMAT(o.StartDate, 'dd MMM yyyy')) AS SessionDay,
                    o.StartDate,
                    CONCAT(FORMAT(o.StartDate, 'ddd · dd MMM yyyy'), '. ',CONCAT(FORMAT(CAST(ss.StartTime AS datetime),'hh:mm tt'),' ', x.abbreviation)) AS ClassStartDate,
                    c.StateId,
                    att.EntityTypeId,
                    c.ClassBookingType,
                    att.[Name] AS [Location],
                    att.[Path] AS StorePath,
                    ag.Id as AgeGroupId,
                    CAST(ag.MinAge AS VARCHAR(10)) + '-' + CAST(ag.MaxAge AS VARCHAR(10)) + ' yrs' as AgeGroup,
                    cg.ColorGroupId,
                    cg.ColorName,
                    cg.HexCode, 
                    '[' + (SELECT STRING_AGG('""' + TRIM(value) + '""', ',') FROM STRING_SPLIT(op.Gender, ',')) + ']' AS GenderList,
                    -- JSON subquery for attendee counts
                    AttendeeCountList = (
                        SELECT 
                            CASE 
                                WHEN ad.[Status] IS NULL OR ad.[Status] = '' THEN 'Pending' 
                                ELSE ad.[Status] 
                            END AS StatusName,
                            COUNT(a.AttendeeId) AS StatusCount
                        FROM JustGoBookingAttendee a
                        INNER JOIN JustGoBookingClassSession cs2 ON a.SessionId = cs2.SessionId
                        INNER JOIN JustGoBookingClassSessionSchedule ss2 ON cs2.SessionId = ss2.SessionId
                        INNER JOIN JustGoBookingScheduleOccurrence so ON ss2.SessionScheduleId = so.ScheduleId
                        INNER JOIN JustGoBookingAttendeeDetails ad 
                            ON so.OccurrenceId = ad.OccurenceId 
                            AND a.AttendeeId = ad.AttendeeId
                        WHERE ad.OccurenceId = o.OccurrenceId
                          AND (ad.AttendeeDetailsStatus IS NULL OR ad.AttendeeDetailsStatus = 1)
                        GROUP BY ad.[Status]
                        FOR JSON PATH
                    ),
                    COUNT(*) OVER() AS TotalCount,   -- Total rows
                    ROW_NUMBER() OVER (ORDER BY o.StartDate ASC,ss.StartTime ASC) AS RowId
     

                FROM  JustGoBookingScheduleOccurrence o
                INNER JOIN JustGoBookingClassSessionSchedule ss ON o.ScheduleId=ss.SessionScheduleId
	            LEFT JOIN  JustGoBookingClassSession cs on cs.SessionId=ss.SessionId
	            LEFT JOIN  JustGoBookingClass c on c.ClassId=cs.ClassId
	            LEFT JOIN JustGoBookingAttachment att ON att.EntityId = c.ClassId AND att.EntityTypeId=1
	            LEFT JOIN DistinctSessionOption op ON cs.SessionId = op.SessionId 
	            LEFT JOIN JustGoBookingClassColorGroup cg ON cs.ColorGroupId = cg.ColorGroupId
	            LEFT JOIN JustGoBookingAgeGroup ag ON cs.AgeGroupId = ag.Id
	            @filterSqlJoin
                LEFT JOIN DaysOfWeek d ON d.DayOfWeek = ss.DayOfWeek

                OUTER APPLY (SELECT TOP 1 gm_offset, abbreviation
							FROM Timezone
							WHERE time_start <= CAST(DATEDIFF(HOUR, '1970-01-01', o.StartDate) AS BIGINT) * 3600
							AND zone_id = cs.TimeZoneId
							ORDER BY time_start DESC) AS x
	            
	            WHERE @sqlWhere
            ),
            AllDataAfterPaging AS (
                SELECT *
                FROM CTEAllData
                WHERE RowId BETWEEN {nextId} AND {dataSize}
            )
            SELECT *
            FROM AllDataAfterPaging 
            ORDER BY RowId;";
        }

    }
}
