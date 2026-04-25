using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Application.Features.Class.V2.Command.BulkAttendanceUpdate;
using MobileApps.Domain.Entities.V2;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V2.Command.SingleAttendanceUpdate
{
    class SingleAttendanceUpdateCommandHandler : IRequestHandler<SingleAttendanceUpdateCommand, bool>
    {
        private readonly LazyService<IWriteRepository<object>> _writeRepository;
        private readonly LazyService<IReadRepository<dynamic>> _readRepository;
        public SingleAttendanceUpdateCommandHandler(LazyService<IWriteRepository<object>> writeRepository, LazyService<IReadRepository<object>> readRepository)
        {
            _writeRepository = writeRepository;
            _readRepository = readRepository;
        }

        public async Task<bool> Handle(SingleAttendanceUpdateCommand request, CancellationToken cancellationToken)
        {
            if (request?.attendance == null)
                return false;

            const string MergeAttendeeSql = @"UPDATE JustGoBookingAttendeeDetails
            SET 
                AttendeeType = @AttendeeType,
                Status = @Status,
                AttendeeDetailsStatus = @AttendeeDetailsStatus,
                CheckedInAt =SYSUTCDATETIME()
            WHERE OccurenceId = @OccurrenceId AND AttendeeId = @AttendeeId;";

            // Step 1: Upsert Attendee
            var queryParameters = new DynamicParameters();
            // For MergeAttendeeSql and SelectAttendeeDetailsIdSql
            queryParameters.Add("@OccurrenceId", request.attendance.OccurrenceId);
            queryParameters.Add("@AttendeeId", request.attendance.AttendeeId);
            queryParameters.Add("@AttendeeType", request.attendance.AttendeeType);
            queryParameters.Add("@Status", request.attendance.Status);
            queryParameters.Add("@AttendeeDetailsStatus", request.attendance.AttendeeDetailsStatus);

            await _writeRepository.Value.ExecuteAsync(
                MergeAttendeeSql,
               queryParameters,
                null,
                "text");

            // Step 2: Get AttendeeDetailsId
            const string SelectAttendeeDetailsIdSql = @"
            SELECT AttendeeDetailsId
            FROM JustGoBookingAttendeeDetails
            WHERE AttendeeId = @AttendeeId AND OccurenceId = @OccurrenceId;";

            dynamic attendeeDetails = await _readRepository.Value.GetAsync(
                SelectAttendeeDetailsIdSql,
                queryParameters,
                null,
                "text");


            // Step 3: Upsert Note
            const string MergeNoteSql = @"
              MERGE INTO JustGoBookingAttendeeDetailNote AS target
              USING (VALUES (@AttendeeDetailsId)) AS source (AttendeeDetailsId)
              ON target.AttendeeDetailsId = source.AttendeeDetailsId
              WHEN MATCHED THEN
                  UPDATE SET 
                      Note = @Note,
                      ModifiedDate = @ModifiedDate
              WHEN NOT MATCHED THEN
              INSERT (AttendeeDetailsId, Note, CreatedDate, ModifiedDate)
              VALUES (@AttendeeDetailsId, @Note, @CreatedDate, @ModifiedDate);";

            if (attendeeDetails != null)
            {
                int id = (int)attendeeDetails.AttendeeDetailsId;
               
                // For MergeNoteSql
                var queryNoteParameters = new DynamicParameters();
                queryNoteParameters.Add("@AttendeeDetailsId", id);
                queryNoteParameters.Add("@Note", request.attendance.Note);
                queryNoteParameters.Add("@ModifiedDate", request.attendance.ModifiedDate);
                queryNoteParameters.Add("@CreatedDate", DateTime.UtcNow);

                await _writeRepository.Value.ExecuteAsync(
                    MergeNoteSql,
                    queryNoteParameters,
                    null,
                    "text");
            }
            return true;
        }

    }
}
