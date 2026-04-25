using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using MobileApps.Application.Features.Event.V2.Queries.GetRecurringOccuranceBookingDateList;
using MobileApps.Application.Features.SystemSetting.Queries.GetGlobalSetting;
using MobileApps.Domain.Entities;
using MobileApps.Domain.Entities.V2.Event;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Event.V2.Commands
{
    class UpdateSingleBookingCheckedStatusCommandHandler : IRequestHandler<UpdateSingleBookingCheckedStatusCommand, Dictionary<string,object>>
    {
        private readonly LazyService<IWriteRepository<string>> _writeRepository;
        private readonly LazyService<IReadRepository<string>> _readRepository;
        private IMediator _mediator;
        public UpdateSingleBookingCheckedStatusCommandHandler(
            LazyService<IWriteRepository<string>> writeRepository,
            LazyService<IReadRepository<string>> readRepository, IMediator mediator)
        {
            _writeRepository = writeRepository;
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<Dictionary<string, object>> Handle(UpdateSingleBookingCheckedStatusCommand request, CancellationToken cancellationToken)
        {
            bool allSuccess = false; // Track overall success
            var checkedInDate = request.CheckedInAt != default ? request.CheckedInAt : DateTime.UtcNow;  
            var returnData = new Dictionary<string, object> {
                { "IsExecute",""},
                { "CheckedInAt",""}
            };

            if (request.CheckingDate == null) return returnData;

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@CourseBookingDocId", request.CourseBookingDocId);
            queryParameters.Add("@AttendanceStatus", request.AttendeeStatus);
            queryParameters.Add("@Note", request.Note);
            queryParameters.Add("@AttendanceDate", request.CheckingDate.Date);
            queryParameters.Add("@CheckedInAt", checkedInDate);

            if (!request.IsRecurringEvent)
            {
                var viewModel = await GetEventDataJson(request);
                queryParameters.Add("@ScheduleTicketRowId", 0);

                if (IsAttendanceNotExist(request.CourseBookingDocId, request.CheckingDate.Date))
                {
                    // Insert if attendance record doesn't exist
                    string insertSql = @"
                            INSERT INTO [dbo].[EventAttendances] (CourseBookingDocId, ScheduleTicketRowId, AttandanceStatus, AttandanceDate,Note,CheckedInAt)
                            VALUES (@CourseBookingDocId, @ScheduleTicketRowId, @AttendanceStatus, @AttendanceDate,@Note,@CheckedInAt)";

                    var insertResult = await _writeRepository.Value.ExecuteAsync(insertSql, queryParameters, null, "text");
                    if (insertResult > 0) allSuccess = true; // If any insert fails, mark as false
                    await CreateOperationLog(1, "Add Attendance ", request.CourseBookingDocId, request.CourseBookingDocId, "Create Event Attendance", viewModel);
                }
                else
                {
                    // Update if attendance record exists
                    string updateSql = @"
                            UPDATE [dbo].[EventAttendances]
                            SET AttandanceStatus = @AttendanceStatus, AttandanceDate = @AttendanceDate, Note=@Note,CheckedInAt=@CheckedInAt
                            WHERE CourseBookingDocId = @CourseBookingDocId";

                    var updateResult = await _writeRepository.Value.ExecuteAsync(updateSql, queryParameters, null, "text");
                    // If any update fails, mark as false
                    if (updateResult > 0) allSuccess = true;

                    await CreateOperationLog(3, "Update Attendance ", request.CourseBookingDocId, request.CourseBookingDocId, "Update Event Attendance", viewModel);
                }
            }
            else
            {
                var viewModel = await GetEventDataJson(request);
                queryParameters.Add("@ScheduleTicketRowId", request.RowId);
                if (CheckValidBookingForRecurring(request.RowId))
                {
                    if (IsAttendanceNotExist(request.CourseBookingDocId, request.CheckingDate.Date, request.IsRecurringEvent, request.RowId))
                    {
                        // Insert if attendance record doesn't exist
                        string insertSql = @"
                            INSERT INTO [dbo].[EventAttendances] (CourseBookingDocId, ScheduleTicketRowId, AttandanceStatus, AttandanceDate,Note,CheckedInAt)
                            VALUES (@CourseBookingDocId, @ScheduleTicketRowId, @AttendanceStatus, @AttendanceDate,@Note,@CheckedInAt)";

                        var insertResult = await _writeRepository.Value.ExecuteAsync(insertSql, queryParameters, null, "text");
                        if (insertResult > 0) allSuccess = true; // If any insert fails, mark as false
                        await CreateOperationLog(1, "Create Attendance ", request.CourseBookingDocId, request.RowId, "Create Recurring Attendance", viewModel);
                    }
                    else
                    {
                        // Update if attendance record exists
                        string updateSql = @"
                            UPDATE [dbo].[EventAttendances]
                            SET AttandanceStatus = @AttendanceStatus, AttandanceDate = @AttendanceDate,Note=@Note,CheckedInAt=@CheckedInAt
                            WHERE CourseBookingDocId = @CourseBookingDocId AND ScheduleTicketRowId=@ScheduleTicketRowId AND  CAST(AttandanceDate AS DATE) = CAST(@AttendanceDate AS DATE)";

                        var updateResult = await _writeRepository.Value.ExecuteAsync(updateSql, queryParameters, null, "text");
                        // If any update fails, mark as false
                        if (updateResult > 0) allSuccess = true;
                        await CreateOperationLog(3, "Update Attendance ", request.CourseBookingDocId, request.RowId, "Update Recurring Attendance", viewModel);
                    }
                }
            }
            returnData["IsExecute"] = allSuccess;
            returnData["CheckedInAt"] = await _mediator.Send(new DateTimeConversionQuery { EventDate= checkedInDate ,TimeZoneId= request.TimeZoneId }, cancellationToken);

            return returnData; // Return true if all operations succeeded, otherwise false
        }

        private bool IsAttendanceNotExist(int docId, DateTime checkingDate, bool isRecurring = false, int rowId = 0)
        {
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@CourseBookingDocId", docId);
            queryParameters.Add("@AttendanceDate", checkingDate);

            string query = "";
            if (isRecurring)
            {
                query = @"SELECT  top 1 * FROM EventAttendances WHERE CourseBookingDocId = @CourseBookingDocId AND CAST(AttandanceDate AS DATE) = CAST(@AttendanceDate AS DATE) AND ScheduleTicketRowId=@ScheduleTicketRowId";
                queryParameters.Add("@ScheduleTicketRowId", rowId);
            }
            else
            {
                query = @"SELECT  top 1 * FROM EventAttendances WHERE CourseBookingDocId = @CourseBookingDocId AND CAST(AttandanceDate AS DATE) = CAST(@AttendanceDate AS DATE)";
            }

            return _readRepository.Value.GetList(query, queryParameters, null, "text").Count() == 0;
        }
        private bool CheckValidBookingForRecurring(int rowId)
        {
            return _mediator.Send(new GetRecurringOccuranceBookingDateListQuery { RowId = rowId }).Result.Count() > 0;
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

        private Task<AttendanceRecord> GetEventDataJson(UpdateSingleBookingCheckedStatusCommand record)
        {
            if (!record.IsRecurringEvent)
            {
                return Task.FromResult(new AttendanceRecord
                {
                    CourseBookingDocId = record.CourseBookingDocId,
                    AttendanceStatus = record.AttendeeStatus,
                    Note = record.Note,
                    AttendanceDate = record.CheckingDate,
                    ScheduleTicketRowId = 0,
                    CheckedInAt = (DateTime)record.CheckedInAt
                });
            }
            else
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
}
