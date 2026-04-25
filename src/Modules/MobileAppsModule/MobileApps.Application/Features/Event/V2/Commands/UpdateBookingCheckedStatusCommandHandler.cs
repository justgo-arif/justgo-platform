using Dapper;
using Json.Patch;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Application.Features.Event.V2.Queries.GetRecurringOccuranceBookingDateList;
using MobileApps.Domain.Entities;
using Newtonsoft.Json;
using MobileApps.Domain.Entities.V2.Event;

namespace MobileApps.Application.Features.Event.V2.Commands
{
    class UpdateBookingCheckedStatusCommandHandler : IRequestHandler<UpdateBookingCheckedStatusCommand, bool>
    {
        private readonly LazyService<IWriteRepository<string>> _writeRepository;
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;
        public UpdateBookingCheckedStatusCommandHandler(
            LazyService<IWriteRepository<string>> writeRepository,
            LazyService<IReadRepository<object>> readRepository, IMediator mediator)
        {
            _writeRepository = writeRepository;
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<bool> Handle(UpdateBookingCheckedStatusCommand request, CancellationToken cancellationToken)
        {
            bool allSuccess = true; // Track overall success
            
            if (!request.IsRecurringEvent)
            {

                foreach (var item in request.UpdateBookingStatuses)
                {
                    var viewModel =await GetEventDataJson(request.IsRecurringEvent, item);
                    if (item.AttendanceDate == null) return false;
                    var queryParameters = new DynamicParameters();
                    queryParameters.Add("@CourseBookingDocId", item.CourseBookingDocId);
                    queryParameters.Add("@AttendanceStatus", item.AttendeeStatus);
                    queryParameters.Add("@Note", item.Note);
                    queryParameters.Add("@AttendanceDate", item.AttendanceDate.Date);
                    queryParameters.Add("@ScheduleTicketRowId", 0);
                    queryParameters.Add("@CheckedInAt", item.CheckedInAt != null ? item.CheckedInAt : DateTime.UtcNow);

                    if (await IsAttendanceNotExist(item.CourseBookingDocId, item.AttendanceDate.Date))
                    {
                        // Insert if attendance record doesn't exist
                        string insertSql = @"
                            INSERT INTO [dbo].[EventAttendances] (CourseBookingDocId, ScheduleTicketRowId, AttandanceStatus, AttandanceDate,Note,CheckedInAt)
                            VALUES (@CourseBookingDocId, @ScheduleTicketRowId, @AttendanceStatus, @AttendanceDate,@Note,@CheckedInAt)";

                        var insertResult = await _writeRepository.Value.ExecuteAsync(insertSql, queryParameters, null, "text");
                        if (insertResult > 0) allSuccess = true; // If any insert fails, mark as false
                        await CreateOperationLog(1, "Add Attendance ", item.CourseBookingDocId, item.CourseBookingDocId,"Create Event Attendance", viewModel);
                    }
                    else
                    {
                        // Update if attendance record exists
                        string updateSql = @"
                            UPDATE [dbo].[EventAttendances]
                            SET AttandanceStatus = @AttendanceStatus, AttandanceDate = @AttendanceDate,   Note=@Note,CheckedInAt=@CheckedInAt
                            WHERE CourseBookingDocId = @CourseBookingDocId";

                        var updateResult = await _writeRepository.Value.ExecuteAsync(updateSql, queryParameters, null, "text");
                        // If any update fails, mark as false
                        if (updateResult > 0) allSuccess = true;
                        await CreateOperationLog(3, "Update Attendance ", item.CourseBookingDocId, item.CourseBookingDocId, "Update Event Attendance", viewModel);
                    }
                }
            }
            else
            {
                foreach (var item in request.UpdateBookingStatuses)
                {
                    var viewModel =await GetEventDataJson(request.IsRecurringEvent, item);
                    var queryParameters = new DynamicParameters();
                    queryParameters.Add("@CourseBookingDocId", item.CourseBookingDocId);
                    queryParameters.Add("@AttendanceStatus", item.AttendeeStatus);
                    queryParameters.Add("@Note", item.Note);
                    queryParameters.Add("@AttendanceDate", item.AttendanceDate.Date);
                    queryParameters.Add("@ScheduleTicketRowId", item.RowId);
                    queryParameters.Add("@CheckedInAt", item.CheckedInAt != null ? item.CheckedInAt : DateTime.UtcNow);

                    if (CheckValidBookingForRecurring(item.RowId))
                    {
                        if (await IsAttendanceNotExist(item.CourseBookingDocId, item.AttendanceDate.Date, request.IsRecurringEvent, item.RowId))
                        {
                            // Insert if attendance record doesn't exist
                            string insertSql = @"
                            INSERT INTO [dbo].[EventAttendances] (CourseBookingDocId, ScheduleTicketRowId, AttandanceStatus, AttandanceDate,Note,CheckedInAt)
                            VALUES (@CourseBookingDocId, @ScheduleTicketRowId, @AttendanceStatus, @AttendanceDate,@Note,@CheckedInAt)";

                            var insertResult = await _writeRepository.Value.ExecuteAsync(insertSql, queryParameters, null, "text");
                            if (insertResult > 0) allSuccess = true; // If any insert fails, mark as false
                            await CreateOperationLog(1, "Create Attendance ", item.CourseBookingDocId, item.RowId, "Create Recurring Attendance", viewModel);
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
                            await CreateOperationLog(3, "Update Attendance ", item.CourseBookingDocId, item.RowId, "Update Recurring Attendance", viewModel);
                        }
                    }
                }
            }

            return allSuccess; // Return true if all operations succeeded, otherwise false
        }
        private async Task<bool> IsAttendanceNotExist(int docId, DateTime checkingDate, bool isRecurring = false, int rowId = 0)
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
            var result = await _readRepository.Value.GetListAsync(query, queryParameters, null, "text");
            return await  Task.FromResult(result.Count() == 0);
        }
        private bool CheckValidBookingForRecurring(int rowId)
        {
            return _mediator.Send(new GetRecurringOccuranceBookingDateListQuery { RowId = rowId }).Result.Count() > 0;
        }

        private async Task CreateOperationLog(int operationType,string OwningEntityType,int AffectedEntity,int OwningEntity,string actionName,AttendanceRecord DataJson)
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

        private Task<AttendanceRecord> GetEventDataJson(bool isRecurring, UpdateBookingStatus record)
        {
            if (isRecurring)
            {
                 return Task.FromResult(new AttendanceRecord
                {
                    CourseBookingDocId = record.CourseBookingDocId,
                    AttendanceStatus = record.AttendeeStatus,
                    Note = record.Note,
                    AttendanceDate = record.AttendanceDate,
                    ScheduleTicketRowId = record.RowId,
                    CheckedInAt = record.CheckedInAt
                });
            }
            else
            {
                 return Task.FromResult(new AttendanceRecord
                {
                    CourseBookingDocId = record.CourseBookingDocId,
                    AttendanceStatus = record.AttendeeStatus,
                    Note = record.Note,
                    AttendanceDate = record.AttendanceDate,
                    ScheduleTicketRowId = 0,
                    CheckedInAt = record.CheckedInAt
                });
            }
           
        }
        
    }
}
