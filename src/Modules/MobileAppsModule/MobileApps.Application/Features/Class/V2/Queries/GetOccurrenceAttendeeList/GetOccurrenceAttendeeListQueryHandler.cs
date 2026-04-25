using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Application.Features.SystemSetting.Queries.GetSystemSettings;
using Newtonsoft.Json;


namespace MobileApps.Application.Features.Class.V2.Queries.GetOccurrenceAttendeeList
{
    class GetOccurrenceAttendeeListQueryHandler : IRequestHandler<GetOccurrenceAttendeeListQuery, IList<IDictionary<string, object>>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public GetOccurrenceAttendeeListQueryHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator
            , ISystemSettingsService systemSettingsService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }
        public async Task<IList<IDictionary<string, object>>> Handle(GetOccurrenceAttendeeListQuery request, CancellationToken cancellationToken)
        {
            string sql = ClassOccurrenceAttendeeListSql();
            string sqlWhere = "ad.OccurenceId=@OccurrenceId AND ISNULL(ad.AttendeeDetailsStatus,1)=1 AND (cs.IsDeleted<>1 AND ss.IsDeleted<>1)";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@OccurrenceId", request.OccurrenceId);

            if (!string.IsNullOrEmpty(request.AttendeeName.Trim()))
            {
                sqlWhere = sqlWhere + @" AND CONCAT(u.FirstName,' ', u.LastName) LIKE '%'+@Filter+'%'";
                queryParameters.Add("@Filter", request.AttendeeName);
            }


            if (request.TicketTypes.Count()>0)
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

            var result = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");



            var bookingList = result.Count()>0? JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(JsonConvert.SerializeObject(result)):null;

            if(bookingList?.Count>0) await GetMemberImage(bookingList, cancellationToken);

            return bookingList;
        }

        private string ClassOccurrenceAttendeeListSql()
        {
            return @"select a.AttendeeId,u.MemberDocId,a.SessionId,so.OccurrenceId,so.StartDate,
                CASE 
                    WHEN sp.ProductId > 0 THEN sp.ProductId
                    ELSE  ISNULL(cp.ProductId, 0)
                END AS ProductId,
                pd.Name as ProductName,pd.ProductReference,sp.ProductType,
                CASE 
                WHEN sp.ProductType=1 THEN 'One-off'
                WHEN sp.ProductType=2 THEN 'Trial'
                WHEN sp.ProductType=3 THEN 'Payg'
                ELSE 'Subscription' 
                END AS ProductTypeName,
                u.FirstName+' '+u.LastName as FullName,u.ProfilePicURL,u.DOB,
                a.Status as AttendeeStatus,
                ad.[Status],dn.Note,ad.AttendeeDetailsStatus,
                ad.AttendeeDetailsId,
                FORMAT(ad.CheckedInAt, 'hh:mm tt, dd MMM yyyy') AS CheckedInAt,
                cs.Name as SessionName,
                (select us.FirstName+' '+us.LastName from  [user] us where us.MemberDocId=ap.BookingEntityId) as BookedBy,
                ap.PaymentDate as BookingDate,
                (select cls.ClassReference from  JustGoBookingClass cls where cls.ClassId=cs.ClassId) as Reference,
                ap.PaymentDate as BookingDate,ap.PaymentId,u.MemberId

                from JustGoBookingAttendee a
                inner join JustGoBookingClassSession cs on a.SessionId=cs.SessionId
                inner join JustGoBookingClassSessionSchedule ss on cs.SessionId=ss.SessionId
                inner join  JustGoBookingScheduleOccurrence so on ss.SessionScheduleId=so.ScheduleId
              
                inner join JustGoBookingAttendeeDetails ad on so.OccurrenceId=ad.OccurenceId AND a.AttendeeId=ad.AttendeeId
                left join JustGoBookingAttendeeDetailNote dn on ad.AttendeeDetailsId=dn.AttendeeDetailsId

                left join JustGoBookingAttendeePayment ap on ad.AttendeePaymentId=ap.AttendeePaymentId
                left join JustGoBookingClassSessionProduct sp on ap.ProductId=sp.ProductId 
                left join Products_Default pd on ap.ProductId=pd.DocId
                LEFT JOIN JustGoBookingClassPricingChartProduct cp on cs.PricingChartId=cp.PricingChartProductId
                inner join Members_Default md on a.EntityDocId=md.DocId
                inner join [user] u on u.MemberDocId = md.DocId

               
                where @sqlWhere";
        }
        private async Task GetMemberImage(IList<IDictionary<string, object>> members, CancellationToken cancellationToken)
        {
            HttpClient _httpClient = new HttpClient();
            var itemKeys = "SYSTEM.AZURESTOREROOT,CLUBPLUS.HOSTSYSTEMID,SYSTEM.SITEADDRESS";
            var systemSettings = await _systemSettingsService.GetSystemSettingsByMultipleItemKey(itemKeys, cancellationToken);
            var storeRoot = systemSettings?.Where(w => w.ItemKey == "SYSTEM.AZURESTOREROOT")?.Select(s => s.Value).SingleOrDefault();
            var hostMid = systemSettings?.Where(w => w.ItemKey == "CLUBPLUS.HOSTSYSTEMID")?.Select(s => s.Value).SingleOrDefault();
            var siteUrl = systemSettings?.Where(w => w.ItemKey == "SYSTEM.SITEADDRESS")?.Select(s => s.Value).SingleOrDefault();
            foreach (var member in members)
            {
                HttpResponseMessage response = null;
                string baseUrl = "";
                string url = "";
                try
                {
                    if (!string.IsNullOrEmpty(member["ProfilePicURL"].ToString()))
                    {
                        baseUrl = storeRoot + "/002/" + hostMid;
                        url = baseUrl + "/User/" + member["Userid"] + "/" + member["ProfilePicURL"];
                        response = await _httpClient.GetAsync(url);
                    }
                    if (string.IsNullOrEmpty(member["ProfilePicURL"].ToString()) || !response.IsSuccessStatusCode)
                    {
                        url = siteUrl + "/Media/Images/";
                        string img = "avatar-" + member["Gender"] + ".png";
                        url = url + img;
                    }

                }
                catch { }

                member["ProfilePicURL"] = url;


            }
        }


    }
}
