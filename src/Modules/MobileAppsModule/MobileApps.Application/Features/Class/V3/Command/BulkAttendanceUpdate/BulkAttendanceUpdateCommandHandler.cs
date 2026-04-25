using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities.V2;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Class.V3.Command.BulkAttendanceUpdate
{
    class BulkAttendanceUpdateCommandHandler : IRequestHandler<BulkAttendanceUpdateCommand, bool>
    {
        private readonly LazyService<IWriteRepository<object>> _writeRepository;
        private readonly LazyService<IReadRepository<object>> _readRepository;
        public BulkAttendanceUpdateCommandHandler(LazyService<IWriteRepository<object>> writeRepository, LazyService<IReadRepository<object>> readRepository)
        {
            _writeRepository = writeRepository;
            _readRepository = readRepository;
        }

        public async Task<bool> Handle(BulkAttendanceUpdateCommand request, CancellationToken cancellationToken)
        {
            
            string MergeAttendeeSql = @"UPDATE JustGoBookingAttendeeDetails
            SET 
                AttendeeType = @AttendeeType,
                Status = @Status,
                AttendeeDetailsStatus = @AttendeeDetailsStatus,
                CheckedInAt =SYSUTCDATETIME()
            WHERE OccurenceId = @OccurrenceId AND AttendeeId = @AttendeeId;";

            string SelectAttendeeDetailsIdSql = @"SELECT AttendeeDetailsId
            FROM JustGoBookingAttendeeDetails
            WHERE AttendeeId = @AttendeeId AND OccurenceId = @OccurrenceId;";

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



            foreach (var item in request.attendanceList)
            {
                var queryParameters = new DynamicParameters();
                // For MergeAttendeeSql and SelectAttendeeDetailsIdSql
                queryParameters.Add("@OccurrenceId", item.OccurrenceId);
                queryParameters.Add("@AttendeeId", item.AttendeeId);
                queryParameters.Add("@AttendeeType", item.AttendeeType);
                queryParameters.Add("@Status", item.Status);
                queryParameters.Add("@AttendeeDetailsStatus", item.AttendeeDetailsStatus);
             
                // Step 1: Upsert Attendee
                await _writeRepository.Value.ExecuteAsync(MergeAttendeeSql, queryParameters, null, "text");

                // Step 2: Get AttendeeDetailsId
                dynamic attendeeDetails = await _readRepository.Value.GetAsync(
                    SelectAttendeeDetailsIdSql,
                    new { item.AttendeeId, item.OccurrenceId }, null, "text");

                if (attendeeDetails != null && !string.IsNullOrEmpty(item.Note))
                {
                    int id = (int)attendeeDetails.AttendeeDetailsId;
                   
                    // Step 3: Upsert Note

                    var queryNoteParameters = new DynamicParameters();
                    queryNoteParameters.Add("@AttendeeDetailsId", id);
                    queryNoteParameters.Add("@Note", item.Note);
                    queryNoteParameters.Add("@ModifiedDate", item.ModifiedDate);
                    queryNoteParameters.Add("@CreatedDate", DateTime.UtcNow);

                    await _writeRepository.Value.ExecuteAsync(
                        MergeNoteSql,
                        queryNoteParameters,
                        null,
                        "text");
                }
            }

            return true;
        }
    }
}
