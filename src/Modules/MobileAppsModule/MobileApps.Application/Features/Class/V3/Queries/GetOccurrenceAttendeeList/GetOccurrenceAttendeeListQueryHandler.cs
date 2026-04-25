using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Microsoft.Data.SqlClient;
using MobileApps.Application.Features.SystemSetting.Queries.GetSystemSettings;
using MobileApps.Domain.Entities.V2.Classes;
using MobileApps.Domain.Entities.V3.Classes;
using Newtonsoft.Json;


namespace MobileApps.Application.Features.Class.V3.Queries.GetOccurrenceAttendeeList
{
    class GetOccurrenceAttendeeListQueryHandler : IRequestHandler<GetOccurrenceAttendeeListQuery, List<AttendeeDto>>
    {
        private readonly LazyService<IReadRepository<AttendeeDto>> _readRepository;
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public GetOccurrenceAttendeeListQueryHandler(LazyService<IReadRepository<AttendeeDto>> readRepository, IMediator mediator
            , ISystemSettingsService systemSettingsService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }
        public async Task<List<AttendeeDto>> Handle(GetOccurrenceAttendeeListQuery request, CancellationToken cancellationToken)
        {
            string sql = ClassOccurrenceAttendeeListSql(request);
            
            string sqlWhere = @"ad.OccurenceId IN @OccurrenceIds AND ISNULL(ad.AttendeeDetailsStatus,1) =1 AND ISNULL(cs.IsDeleted,0)=0";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@OccurrenceIds", request.OccurrenceIds);

            if (!string.IsNullOrEmpty(request.AttendeeName.Trim()))
            {
                sqlWhere = sqlWhere + @" AND CONCAT(u.FirstName,' ', u.LastName) LIKE '%'+@Filter+'%'";
                queryParameters.Add("@Filter", request.AttendeeName);
            }


            if (request.TicketTypes.Count() > 0)
            {
                sqlWhere = sqlWhere + @" AND sp.ProductType IN @TicketTypeIds";
                queryParameters.Add("@TicketTypeIds", request.TicketTypes);
            }

            if (request.AttendeeStatuses.Count() > 0)
            {

                if (request.AttendeeStatuses.Count() == 1 && request.AttendeeStatuses.Contains("pending", StringComparer.OrdinalIgnoreCase))
                {
                    sqlWhere = sqlWhere + @" AND (ad.Status IS NULL OR ad.Status = '' OR ad.Status = 'Pending')";
                }
                else if (request.AttendeeStatuses.Count() > 1 && request.AttendeeStatuses.Contains("pending", StringComparer.OrdinalIgnoreCase))
                {
                    sqlWhere = sqlWhere + @" AND (ad.Status IN @StatusList OR ad.Status IS NULL OR ad.Status = '')";
                    queryParameters.Add("@StatusList", request.AttendeeStatuses);
                }
                else
                {
                    sqlWhere = sqlWhere + @" AND (ad.Status IN @StatusList)";
                    queryParameters.Add("@StatusList", request.AttendeeStatuses);
                }
            }

            sql = sql.Replace("@sqlWhere", sqlWhere);

            var result=await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");

            return result.AsList();
        }

        private string ClassOccurrenceAttendeeListSql(GetOccurrenceAttendeeListQuery request)
        {
            int nextId = request.NextId > 0 ? request.NextId + 1 : 0;
            int dataSize = request.DataSize > 0 ? (request.NextId + request.DataSize) : 100;
            return $@";WITH AllAttendeeList AS (
            SELECT 
                a.AttendeeId,
                u.MemberDocId,
                a.SessionId,
                so.OccurrenceId,
                CASE 
                    WHEN sp.ProductId > 0 THEN sp.ProductId
                    ELSE ISNULL(cp.ProductId, 0)
                END AS ProductId,
                pd.Name AS ProductName,
                pd.ProductReference,
                sp.ProductType,
                CASE 
                    WHEN sp.ProductType = 1 THEN 'One-off'
                    WHEN sp.ProductType = 2 THEN 'Trial'
                    WHEN sp.ProductType = 3 THEN 'Payg'
                    ELSE 'Subscription' 
                END AS ProductTypeName,
                u.FirstName + ' ' + u.LastName AS FullName,
                ss.DayOfWeek,
                so.StartDate,
                u.ProfilePicURL,
                u.UserId,
                u.Gender,
                u.DOB,
                u.UserSyncId,
                a.Status AS AttendeeStatus,
                ad.[Status],
                dn.Note,
                ISNULL(dn.AttendeeDetailNoteId, 0) as AttendeeDetailNoteId,
                ad.AttendeeDetailsStatus,
                ad.AttendeeDetailsId,
                cs.TimeZoneId,
                ad.CheckedInAt,
                ad.AttendeeType,
                cs.Name AS SessionName,
                usBookedBy.FirstName + ' ' + usBookedBy.LastName AS BookedBy,
                cls.ClassReference AS Reference,
                ap.PaymentDate AS BookingDate,
                ap.PaymentId,
                u.MemberId

            FROM JustGoBookingAttendee a
            INNER JOIN JustGoBookingClassSession cs ON a.SessionId = cs.SessionId
            INNER JOIN JustGoBookingClassSessionSchedule ss ON cs.SessionId = ss.SessionId
            INNER JOIN JustGoBookingScheduleOccurrence so ON ss.SessionScheduleId = so.ScheduleId
            INNER JOIN JustGoBookingAttendeeDetails ad 
                ON so.OccurrenceId = ad.OccurenceId 
                AND a.AttendeeId = ad.AttendeeId
            LEFT JOIN JustGoBookingAttendeeDetailNote dn 
                ON ad.AttendeeDetailsId = dn.AttendeeDetailsId
            LEFT JOIN JustGoBookingAttendeePayment ap 
                ON ad.AttendeePaymentId = ap.AttendeePaymentId
            LEFT JOIN JustGoBookingClassSessionProduct sp 
                ON ap.ProductId = sp.ProductId 
            LEFT JOIN Products_Default pd 
                ON ap.ProductId = pd.DocId
            LEFT JOIN JustGoBookingClassPricingChartProduct cp 
                ON cs.PricingChartId = cp.PricingChartProductId
            INNER JOIN Members_Default md 
                ON a.EntityDocId = md.DocId
            INNER JOIN [user] u 
                ON u.MemberDocId = md.DocId
            LEFT JOIN [user] usBookedBy 
                ON usBookedBy.MemberDocId = ap.BookingEntityId
            LEFT JOIN JustGoBookingClass cls 
                ON cls.ClassId = cs.ClassId
            -- Add useful filters to reduce row count as early as possible!
                WHERE @sqlWhere
        ),

        NumberedCTE AS (
            SELECT *,
                ROW_NUMBER() OVER (ORDER BY FullName {request.SortOrder}) AS RowId,
                COUNT(*) OVER () AS TotalCount
            FROM AllAttendeeList
        ),

        PagedCTE AS (
            SELECT *
            FROM NumberedCTE
            WHERE  RowId BETWEEN {nextId} AND {dataSize} -- <-- Adjust for your actual page window needed!
        )
        ,UserNoteFlags AS (
            SELECT
                mn.EntityId AS UserId,
                MAX(CASE WHEN nc.NoteCategoryName = 'Alert' THEN 1 ELSE 0 END) AS IsAlert,
                MAX(CASE WHEN nc.NoteCategoryName = 'Medical' THEN 1 ELSE 0 END) AS IsMedical
            FROM MemberNotes mn
            INNER JOIN NoteCategories nc
                ON mn.NoteCategoryId = nc.NoteCategoryId
            WHERE nc.IsActive = 1
                AND mn.IsActive = 1
                AND mn.IsHide = 0
            GROUP BY mn.EntityId
        ),
        UserPhotoConsent as (
              SELECT  
                  up.UserID AS UserId,
                  ISNULL(MAX(CASE WHEN up.PreferenceValue = 'false' THEN 1 ELSE 0 END), 1) AS IsPhotoConsent
              FROM UserPreferences up
              INNER JOIN PreferenceTypes pt
                  ON up.PreferenceTypeID = pt.PreferenceTypeID 
              INNER JOIN PreferenceCategories pc
                  ON pt.PreferenceCategoryID = pc.PreferenceCategoryID
              WHERE up.OrganizationID = {request.ClubId}
              AND LOWER(pt.PreferenceName) = 'photoconcent'
              AND pt.IsActive = 1
              AND up.IsActive = 1
              AND pc.IsActive = 1
              GROUP BY up.UserID
        )
        SELECT 
            pc.AttendeeId,
            pc.MemberDocId,
            pc.UserSyncId,
            pc.SessionId,
            pc.OccurrenceId,
            pc.ProductId,
            pc.ProductName,
            pc.ProductReference,
            pc.ProductType,
            pc.ProductTypeName,
            pc.FullName,
            pc.DayOfWeek,
            pc.StartDate,
            pc.ProfilePicURL,
            pc.UserId,
            pc.Gender,
            pc.DOB,
            pc.AttendeeStatus,
            pc.Status,
            pc.Note,
            pc.AttendeeDetailNoteId,
            pc.AttendeeDetailsStatus,
            pc.AttendeeDetailsId,
            pc.TimeZoneId,
            pc.CheckedInAt,
            pc.AttendeeType,
            pc.SessionName,
            pc.BookedBy,
            pc.Reference,
            pc.BookingDate,
            pc.PaymentId,
            pc.MemberId,
            uf.IsAlert,
            uf.IsMedical,
            COALESCE(uf.IsAlert, 0) AS IsAlertNote,
            COALESCE(uf.IsMedical, 0) AS IsMedicalNote,
            ISNULL(ph.IsPhotoConsent, 0) AS IsPhotoConsent,
            pc.RowId,
            pc.TotalCount
 
        FROM PagedCTE pc
        LEFT JOIN UserNoteFlags uf
            ON pc.UserId = uf.UserId

        LEFT JOIN UserPhotoConsent ph
            ON pc.UserId = ph.UserId
        ORDER BY pc.RowId;";
        }

    }
}
