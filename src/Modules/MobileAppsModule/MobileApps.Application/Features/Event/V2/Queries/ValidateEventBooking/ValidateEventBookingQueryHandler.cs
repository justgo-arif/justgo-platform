using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using MobileApps.Application.Features.Event.V2.Commands;
using MobileApps.Application.Features.SystemSetting.Queries.GetGlobalSetting;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Event.V2.Queries.ValidateEventBooking
{
    class ValidateEventBookingQueryHandler : IRequestHandler<ValidateEventBookingQuery, Tuple<IDictionary<string, object>, bool>>
    {
        private readonly LazyService<IReadRepository<object>> _readObjRepository;
        private readonly LazyService<IReadRepository<string>> _readRepository;
        private readonly LazyService<IWriteRepository<object>> _writeRepository;
        private readonly ISystemSettingsService _systemSettingsService;
        private IMediator _mediator;

        public ValidateEventBookingQueryHandler(LazyService<IReadRepository<object>> readObjRepository, LazyService<IReadRepository<string>> readRepository
            , LazyService<IWriteRepository<object>> writeRepository, IMediator mediator
            , ISystemSettingsService systemSettingsService)
        {
            _readObjRepository = readObjRepository;
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }

        public async Task<Tuple<IDictionary<string, object>, bool>> Handle(ValidateEventBookingQuery request, CancellationToken cancellationToken)
        {
            IDictionary<string, object> resultData = new Dictionary<string, object>();
            bool isCheckedIn = false;

            if (request.DocId > 0)
            {

                string sqlCourse = $@"select DocId from CourseBooking_Default where DocId=@DocumentDocId";

                var queryParametersCourse = new DynamicParameters();
                queryParametersCourse.Add("@DocumentDocId", request.DocId);

                var result = await _readRepository.Value.GetAsync(sqlCourse, queryParametersCourse, null, "text");

                if (!string.IsNullOrEmpty(result))
                {
                    var queryParametersAttendy = new DynamicParameters();
                    queryParametersAttendy.Add("@CourseBookingDocId", Convert.ToInt32(result));
                    
                    //update booking status
                    if (IsNotChecking(Convert.ToInt32(result)))
                    {
                        await UpdateBookingCheckedInStatus(Convert.ToInt32(result));
                            await AttendanceEntry(Convert.ToInt32(result), request.BookingDate, request.CheckedInAt);
                    }
                    else
                        isCheckedIn = true;


                    var attendee= await _readObjRepository.Value.GetAsync(EventAttendeeSql(), queryParametersAttendy, null, "text");

                    var data = JsonConvert.DeserializeObject<IDictionary<string, object>>(JsonConvert.SerializeObject(attendee));

                    if (data != null) await GetUserImage(data,cancellationToken);

                    resultData = new Dictionary<string, object>
                        {
                            { "CheckInTime",  request.BookingDate.Date.ToString("hh:mm tt, dd MMM yyyy")},
                            { "ProfilePicURL", data["ProfilePicURL"].ToString() },
                            { "Name", data["UserName"].ToString() },
                            { "EventName", data["EventName"].ToString() },
                            { "Product",data["Product"].ToString() },
                            { "CheckedInAt", data["CheckedInAt"].ToString()},
                            { "TicketCount",data["Quantity"].ToString() },
                            { "Gift", "" },
                            { "Remarks", "" },
                            { "AlreadyCheckedIn", isCheckedIn },
                            { "IsRestricted", Convert.ToBoolean(data["IsLocked"].ToString()) }
                        };

                   
                }

            }


            return Tuple.Create(resultData, false);
        }

        
        private string EventAttendeeSql()
        {
            return @"-- Get detailed booking/user/event info for a specific course booking
            SELECT DISTINCT
                u.Userid,
                u.Gender,
                CONCAT(u.FirstName, ' ', u.LastName) AS UserName,
                ISNULL(u.ProfilePicURL, '') AS ProfilePicURL,
                u.MemberId AS Mid,
                ed.EventName,
                pd.[Name] AS Product,
                cbd.DocId AS CourseDocId,
                cbd.Coursebookingid,
                pr.CurrentStateId,
                st.[Name] AS StateName,
                ed.Timezone,
                CAST(cbd.Quantity AS INT) AS Quantity,
                u.IsLocked,
                FORMAT(
                    CAST(dbo.[GET_UTC_LOCAL_DATE_TIME](ea.CheckedInAt, ed.TimeZone) AS DateTime),
                    'hh:mm tt, dd MMM yyyy'
                ) AS CheckedInAt
            FROM
                CourseBooking_Default cbd
                INNER JOIN Products_Default pd ON pd.DocId = cbd.Productdocid
                INNER JOIN Events_Default ed ON ed.DocId = cbd.Coursedocid
                INNER JOIN [user] u ON u.MemberDocId = cbd.Entityid
                LEFT JOIN EventAttendances ea ON ea.CourseBookingDocId = cbd.DocId 
                INNER JOIN ProcessInfo pr ON cbd.DocId = pr.PrimaryDocId
                INNER JOIN [state] st ON st.StateId = pr.CurrentStateId
            Where cbd.DocId=@CourseBookingDocId";
        }

        private async Task UpdateBookingCheckedInStatus(int CourseBookingDocId)
        {
            string sql = @"UPDATE CourseBooking_Default
                        SET Checkedin = 1
                        Where DocId = @CourseBookingDocId";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@CourseBookingDocId", CourseBookingDocId);

            await _writeRepository.Value.ExecuteAsync(sql, queryParameters, null, "text");
        }

        private bool IsNotChecking(int CourseBookingDocId)  
        {
            //if data null or empty then return not check in true 
            string sql = @"select DocId  from CourseBooking_Default  where DocId=@CourseBookingDocId AND Checkedin=1";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@CourseBookingDocId", CourseBookingDocId);

            var data = _readRepository.Value.GetAsync(sql, queryParameters, null, "text").Result;

            return  string.IsNullOrEmpty(data);
        }

        private async Task AttendanceEntry(int docId,DateTime bookingDate,DateTime checkedInAt)
        {
            var dataCommand = new List<UpdateBookingStatus>
                {
                    new UpdateBookingStatus
                    {
                        CourseBookingDocId = docId,
                        AttendeeStatus="Checked In",
                        AttendanceDate=bookingDate,
                        CheckedInAt=checkedInAt
                    }
                };
                        
            await _mediator.Send(new UpdateBookingCheckedStatusCommand(dataCommand));
        }

        private async Task GetUserImage(IDictionary<string, object> member, CancellationToken cancellationToken)
        {
            try
            {
                var itemKeys = "SYSTEM.AZURESTOREROOT,CLUBPLUS.HOSTSYSTEMID,SYSTEM.SITEADDRESS";
                var systemSettings = await _systemSettingsService.GetSystemSettingsByMultipleItemKey(itemKeys, cancellationToken);
                var storeRoot = systemSettings?.Where(w => w.ItemKey == "SYSTEM.AZURESTOREROOT")?.Select(s => s.Value).SingleOrDefault();
                var hostMid = systemSettings?.Where(w => w.ItemKey == "CLUBPLUS.HOSTSYSTEMID")?.Select(s => s.Value).SingleOrDefault();
                var siteUrl = systemSettings?.Where(w => w.ItemKey == "SYSTEM.SITEADDRESS")?.Select(s => s.Value).SingleOrDefault();

                string baseUrl = "";
                string url = "";
                HttpResponseMessage response = null;
                HttpClient _httpClient = new HttpClient();
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

                member["ProfilePicURL"] = url;
            }
            catch
            {

               
            }
            
        }
    }
}
