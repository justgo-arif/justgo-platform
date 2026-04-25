using System.Threading;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using MobileApps.Application.Features.SystemSetting.Queries;
using MobileApps.Application.Features.SystemSetting.Queries.GetGlobalSetting;
using MobileApps.Application.Features.SystemSetting.Queries.GetSystemSettings;
using MobileApps.Domain.Entities;
using MobileApps.Domain.Entities.V2.Event;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Event.V2.Commands
{
    class SetAttendenceForOccuranceBookingCommandHandler : IRequestHandler<SetAttendenceForOccuranceBookingCommand, IDictionary<string, object>>
    {
        private readonly LazyService<IWriteRepository<string>> _writeRepository;
        private readonly LazyService<IReadRepository<object>> _readObjRepository;
        private readonly LazyService<IReadRepository<string>> _readRepository;
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;

        public SetAttendenceForOccuranceBookingCommandHandler(
            LazyService<IWriteRepository<string>> writeRepository,
            LazyService<IReadRepository<string>> readRepository, LazyService<IReadRepository<object>> readObjRepository, IMediator mediator
            , ISystemSettingsService systemSettingsService)
        {
            _writeRepository = writeRepository;
            _readRepository = readRepository;
            _readObjRepository = readObjRepository;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }

        public async Task<IDictionary<string, object>> Handle(SetAttendenceForOccuranceBookingCommand request, CancellationToken cancellationToken)
        {
            var resultData = new Dictionary<string, object>();
            bool isAlreadyCheckedIn = false;
            bool allSuccess = false; // Track overall success
          
            if (request.CheckingDate == default)
            {
                resultData.Add("IsExecute", allSuccess);
                return resultData;
            }

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@CourseBookingDocId", request.CourseBookingDocId);
            queryParameters.Add("@AttendanceStatus", request.AttendeeStatus);
            queryParameters.Add("@Note", request.Note);
            queryParameters.Add("@AttendanceDate", request.CheckingDate.Date);
            queryParameters.Add("@CheckedInAt", request.CheckedInAt);


            var viewModel = await GetEventDataJson(request);
            queryParameters.Add("@ScheduleTicketRowId", request.RowId);
            var data = await GetAttendance(request.CourseBookingDocId, request.CheckingDate, request.RowId);
            if (data == null)
            {
                //attendance record doesn't exist
                string insertSql = @"
                            INSERT INTO [dbo].[EventAttendances] (CourseBookingDocId, ScheduleTicketRowId, AttandanceStatus, AttandanceDate,Note,CheckedInAt)
                            VALUES (@CourseBookingDocId, @ScheduleTicketRowId, @AttendanceStatus, @AttendanceDate,@Note,@CheckedInAt)";

                var insertResult = await _writeRepository.Value.ExecuteAsync(insertSql, queryParameters, null, "text");
                if (insertResult > 0) allSuccess = true; // If any insert fails, mark as false
                await CreateOperationLog(1, "Create Attendance ", request.CourseBookingDocId, request.RowId, "Create Recurring Attendance", viewModel);
            }
            else if (data != null && data.AttandanceStatus == "Pending")
            {
                //attendance record  exist but pending
                string updateSql = @"Update EventAttendances set AttandanceStatus=@AttendanceStatus
                                            WHERE CourseBookingDocId = @CourseBookingDocId 
                                            AND CAST(AttandanceDate AS DATE) = CAST(@AttendanceDate AS DATE) 
                                            AND ScheduleTicketRowId=@ScheduleTicketRowId";

                var updateResult = await _writeRepository.Value.ExecuteAsync(updateSql, queryParameters, null, "text");
                if (updateResult > 0) allSuccess = true; // If any insert fails, mark as false
                await CreateOperationLog(3, "Update Attendance ", request.CourseBookingDocId, request.RowId, "Update Recurring Attendance", viewModel);

            }
            else isAlreadyCheckedIn = true;


            if (allSuccess || isAlreadyCheckedIn)
            {

                var attendyParameters = new DynamicParameters();
                attendyParameters.Add("@CourseBookingDocId", request.CourseBookingDocId);
                var attendy = await _readObjRepository.Value.GetListAsync(RecuranceOccuranceAttendySql(), attendyParameters, null, "text");

                var dataAttendee = JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(JsonConvert.SerializeObject(attendy));

                if (dataAttendee.Count > 0)
                {

                    await GetUserImage(dataAttendee[0],cancellationToken);
                    int isCheckedin = (allSuccess || isAlreadyCheckedIn) ? 1 : 0;

                    resultData = new Dictionary<string, object>{
                        { "CheckInTime", request.CheckingDate.Date.ToString("hh:mm tt, dd MMM yyyy")},
                        { "ProfilePicURL", dataAttendee.FirstOrDefault()["ProfilePicURL"].ToString() },
                        { "Name", dataAttendee.FirstOrDefault()["UserName"].ToString() },
                        { "EventName", dataAttendee.FirstOrDefault()["EventName"].ToString() },
                        { "Product",dataAttendee.FirstOrDefault()["Ticket"].ToString() },
                        { "CheckedInAt",dataAttendee.FirstOrDefault()["CheckedInAt"].ToString()},
                        { "TicketCount", dataAttendee.FirstOrDefault()["TicketCount"].ToString() },
                        { "Gift", "" },
                        { "Remarks", "" },
                        { "Checkedin", isCheckedin },
                        { "AlreadyCheckedIn", isAlreadyCheckedIn },
                        { "IsRestricted", false },
                        {"IsExecute",allSuccess}
                    };
                }
            }
            return resultData;
        }

        private async Task<EventAttendancesViewModel?> GetAttendance(int docId, DateTime? checkingDate, int rowId = 0)
        {


            if (!checkingDate.HasValue) return null;
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@CourseBookingDocId", docId);
            queryParameters.Add("@AttendanceDate", checkingDate);
            queryParameters.Add("@ScheduleTicketRowId", rowId);
            string query = "";

            query = @"SELECT TOP 1 * FROM EventAttendances WHERE CourseBookingDocId = @CourseBookingDocId AND CAST(AttandanceDate AS DATE) = CAST(@AttendanceDate AS DATE) AND ScheduleTicketRowId=@ScheduleTicketRowId order by Id desc;";


            var data = await _readObjRepository.Value.GetAsync(query, queryParameters, null, "text");

            return data != null ? JsonConvert.DeserializeObject<EventAttendancesViewModel>(JsonConvert.SerializeObject(data)) : null;
        }
        private string RecuranceOccuranceAttendySql()
        {
            return @";WITH RankedCTE AS (
            SELECT
                CourseBooking_Default.DocId AS CourseBookingDocId,
                CAST(EventRecurringScheduleInterval.RowId AS BIGINT) AS RowId,
                CONCAT([user].FirstName, ' ', [user].LastName) AS UserName,
                [user].Userid as UserId,
                [user].Gender,
                ISNULL([user].ProfilePicURL, '') AS ProfilePicURL,
                Events_Default.EventName,
                Products_Default.Name AS Ticket,
                CourseBooking_Default.Quantity AS TicketCount,
                Events_Default.RepositoryId,
                EventAttendances.AttandanceDate,
                EventAttendances.AttandanceStatus,
                ISNULL(EventAttendances.Note, '') AS Note,
                CAST(ISNULL(CourseBooking_Default.Checkedin, 0) AS INT) AS Checkedin,
                Events_Default.Timezone,
                EventRecurringScheduleInterval.RecurringStartTime,
                FORMAT(EventRecurringScheduleInterval.ScheduleDate,'yyyy-MM-dd') AS ScheduleDate,
                EventRecurringScheduleInterval.RecurringEndTime,
                FORMAT(EventRecurringScheduleInterval.ScheduleEndDate,'yyyy-MM-dd') AS ScheduleEndDate,
                EventAttendances.CheckedInAt,

                ROW_NUMBER() OVER (
                    PARTITION BY CourseBooking_Default.DocId
                    ORDER BY  CourseBooking_Default.DocId DESC
                ) AS rn

            FROM CourseBooking_Default
            INNER JOIN EventRecurringScheduleTicket
                ON EventRecurringScheduleTicket.TicketDocId = CourseBooking_Default.Productdocid
            INNER JOIN Products_Default
                ON Products_Default.DocId = EventRecurringScheduleTicket.TicketDocId
            INNER JOIN EventRecurringScheduleInterval
                ON EventRecurringScheduleTicket.EventRecurringScheduleIntervalRowId = EventRecurringScheduleInterval.RowId
            INNER JOIN Events_Default
                ON Events_Default.DocId = EventRecurringScheduleInterval.EventDocId
            --new start
            INNER JOIN Document dd
                ON CourseBooking_Default.DocId = dd.DocId
            INNER JOIN Members_Default md
                ON md.DocId = CourseBooking_Default.Entityid
            --new end
            INNER JOIN Document mdoc
                ON mdoc.DocId = md.DocId
            INNER JOIN [user]
                ON [user].MemberDocId = md.DocId
            INNER JOIN ProcessInfo
                ON CourseBooking_Default.DocId = ProcessInfo.PrimaryDocId
            INNER JOIN [state]
                ON [State].StateId = ProcessInfo.CurrentStateId
            LEFT JOIN EventAttendances
                ON EventAttendances.CourseBookingDocId = CourseBooking_Default.DocId
            
            WHERE Events_Default.Isrecurring = 1
              AND [State].StateId IN (23,24,25)
              AND CourseBooking_Default.DocId = @CourseBookingDocId
        )
        SELECT
            CourseBookingDocId,
            RowId,
            UserName,
            Userid,
            Gender,
            ProfilePicURL,
            EventName,
            Ticket,
            TicketCount,
            RepositoryId,
            AttandanceDate,
            AttandanceStatus,
            Note,
            Checkedin,
            Timezone,
            RecurringStartTime,
            ScheduleDate,
            RecurringEndTime,
            ScheduleEndDate,
            dbo.[GET_UTC_LOCAL_DATE_TIME](CAST(ScheduleDate AS DATETIME) + CAST(RecurringStartTime AS DATETIME),Timezone) AS StartDate,
            dbo.[GET_UTC_LOCAL_DATE_TIME](CAST(ScheduleEndDate AS DATETIME) + CAST(RecurringEndTime AS DATETIME),Timezone) AS EndDate,
            FORMAT(dbo.[GET_UTC_LOCAL_DATE_TIME](CheckedInAt,Timezone), 'hh:mm tt, dd MMM yyyy') AS CheckedInAt
        FROM RankedCTE
        WHERE rn = 1;";
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

        //log related Method
        private async Task CreateOperationLog(int operationType, string OwningEntityType, int AffectedEntity, int OwningEntity, string actionName, AttendanceRecord DataJson)
        {
            var jsonData = JsonConvert.SerializeObject(DataJson);

            string sql = @" --log for member post
                DECLARE @NewId INT = 0;
                DECLARE @EventJson NVARCHAR(MAX) = ''; -- Increased length for larger JSON
                DECLARE @EventDataJson NVARCHAR(MAX) = ''; -- Increased length for larger JSON
                DECLARE @ActionName NVARCHAR(255) = '';
                DECLARE @OwningEntityId INT;
                DECLARE @Status NVARCHAR(255) = '';

                SET @ActionName = @ActionNameParam;

                -- Insert into SystemEvent
                INSERT INTO [SystemEvent] ([Category], [SubCategory], [Action], [ActionUserId], [AffectedEntityId], [AffectedEntityType], [AuditDate], [OwningEntityId])
                VALUES (0, 0, @OperationType, 1, @AffectedEntity, 0, SYSUTCDATETIME(), @OwningEntity);

                SET @NewId = SCOPE_IDENTITY(); -- Use SCOPE_IDENTITY instead of @@IDENTITY for better accuracy

                -- Generate EventJson
                SET @EventJson = (SELECT 'JustGoApp' AS [Source], 'Created' AS [Type], AffectedEntityId, OwningEntityId, @OwningEntityType AS OwningEntitydType, AuditDate, @ActionName AS ActionName
                    FROM [SystemEvent] WHERE Id = @NewId FOR JSON AUTO, WITHOUT_ARRAY_WRAPPER);

                -- Generate EventDataJson
                SET @EventDataJson = @EventDataJsonModel;

                -- Insert into SystemEventData
                INSERT INTO [SystemEventData] (Id, [Name], [Value])
                VALUES (@NewId, @ActionName, JSON_MODIFY(@EventJson, '$.CurrentData', @EventDataJson));
                --end log";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@OperationType", operationType);
            queryParameters.Add("@AffectedEntity", AffectedEntity);
            queryParameters.Add("@OwningEntity", OwningEntity);
            queryParameters.Add("@OwningEntityType", OwningEntityType);
            queryParameters.Add("@ActionNameParam", actionName);
            queryParameters.Add("@EventDataJsonModel", jsonData);
            await _writeRepository.Value.ExecuteAsync(sql, queryParameters, null, "text");
        }

        private Task<AttendanceRecord> GetEventDataJson(SetAttendenceForOccuranceBookingCommand record)
        {
            return Task.FromResult(new AttendanceRecord
            {
                CourseBookingDocId = record.CourseBookingDocId,
                AttendanceStatus = record.AttendeeStatus,
                Note = record.Note,
                AttendanceDate = record.CheckingDate,
                ScheduleTicketRowId = record.RowId,
                CheckedInAt = (DateTime)record.CheckedInAt
            });

        }
    }
}
