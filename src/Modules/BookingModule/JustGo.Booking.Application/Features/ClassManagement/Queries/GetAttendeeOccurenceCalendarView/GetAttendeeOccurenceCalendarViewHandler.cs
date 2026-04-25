using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Booking.Application.DTOs.ClassManagementDtos;
using Newtonsoft.Json;
using System.Data;

namespace JustGo.Booking.Application.Features.ClassManagement.Queries.GetAttendeeOccurenceCalendarView
{

    public class GetAttendeeOccurenceCalendarViewHandler(IReadRepositoryFactory readRepository)
        : IRequestHandler<GetAttendeeOccurenceCalendarViewQuery, Result<CalendarViewResponseDto>>
    {
        private const string FORMAT_DATE = "yyyy-MM-dd HH:mm:ss";
        private readonly IReadRepositoryFactory _readRepository = readRepository;


        public async Task<Result<CalendarViewResponseDto>> Handle(
            GetAttendeeOccurenceCalendarViewQuery request,
            CancellationToken cancellationToken)
        {
            var req = request.CalendarRequest;

            var sessionRepo = _readRepository.GetLazyRepository<JustGoBookingClassSessionInfoCalendarView>().Value;
            var session = await GetScheduleInfoBySessionId(req.SessionGuid, sessionRepo, cancellationToken);

            if (session.SessionId == 0)
            {
                return Result<CalendarViewResponseDto>.Failure("Session not found.", ErrorType.BadRequest);
            }

            // Step 2: Get Attendance List
            var attendanceRepo = _readRepository.GetLazyRepository<object>().Value;
            var (attendees, listofDates, totalCount) = await GetAttendanceList(
                session.SessionId,
                req.OccurrenceIds,
                req.PageNumber,
                req.RowsPerPage,
                attendanceRepo,
                cancellationToken,
                req.FilterValue ?? string.Empty,
                req.IsActiveMemberOnly);

            // Step 3: Get Event Attendances and Notes
            var attendanceStatusRepo = _readRepository.GetLazyRepository<AttendeeStatus>().Value;
            var eventAttendances = await GetEventAttendanceAndNotes(session.SessionId,req.OccurrenceIds, attendanceStatusRepo, cancellationToken);

            // Step 4: Get Chip Statuses
            var chipStatusRepo = _readRepository.GetLazyRepository<object>().Value;
            var attendeeChipStatuses = await AttendeeChipStatusBySessionId(session.SessionId, req.OccurrenceIds, null, chipStatusRepo, cancellationToken);
            // Step 5: Build Attendee Calendar DTOs
            var attendeeCalendarDtos = await BuildAttendeeCalendarDtos(
                attendees,
                eventAttendances,
                session.ClassGuid,
                cancellationToken);

            // Step 6: Generate List of Dates from Session Schedules
            var listOfDates = await GenerateScheduleOccurrences(session.SessionSchedules, req.OccurrenceIds, cancellationToken);

            // Step 7: Get Emergency Contacts
            var memberIds = string.Join(",", attendeeCalendarDtos.Select(x => x.Entityid).Distinct());
            var emergencyContactRepo = _readRepository.GetLazyRepository<MemberEmergencyContact>().Value;
            var contacts = !string.IsNullOrEmpty(memberIds)
                ? await GetEmergencyContacts(memberIds, emergencyContactRepo, cancellationToken)
                : new List<MemberEmergencyContact>();

            // Step 8: Build Combined Result
            var combinedResult = BuildCombinedResult(attendeeCalendarDtos, attendeeChipStatuses, contacts);

            // Step 9: Build Schedule Info
            var sessionInfo = new JustGoBookingClassSessionInfoCalendarView
            {
                SessionId = session.SessionId,
                SessionName = session.SessionName,
                ClassSessionGuid = session.ClassSessionGuid,
                SessionReference = session.SessionReference,
                ClassId = session.ClassId,
                ClassName = session.ClassName,
                ClassGuid = session.ClassGuid,
                ClassReference = session.ClassReference,
                AgeGroupName = session.AgeGroupName,
                ColorName = session.ColorName,
                ColorHexCode = session.ColorHexCode,
                EnableHourlyRate = session.EnableHourlyRate,
                SessionPriceOptions = session.SessionPriceOptions
            };

            var response = new CalendarViewResponseDto
            {
                SessionInfo = sessionInfo,
                Attendees = combinedResult,
                ListOfDates = listOfDates.OrderBy(o => o.StartDate).ToList(),
                TotalCount = totalCount
            };

            return response;

        }

        public async Task<JustGoBookingClassSessionInfoCalendarView> GetScheduleInfoBySessionId(
     Guid sessionGuid,
     IReadRepository<JustGoBookingClassSessionInfoCalendarView> repo,
     CancellationToken cancellationToken)
        {
            const string query = """
                                 DECLARE @SessionId INT;

                                 SELECT @SessionId = s.SessionId
                                 FROM JustGoBookingClassSession AS s
                                 WHERE s.ClassSessionGuid = @SessionGuid;

                                 SELECT 
                                     s.SessionId,
                                     s.Name AS SessionName,
                                     s.ClassSessionGuid,
                                     s.SessionReference,
                                     ag.Name AS AgeGroupName,
                                     ccg.ColorName,
                                     ccg.HexCode AS ColorHexCode,
                                     c.ClassId,
                                     c.Name AS ClassName,
                                     c.ClassGuid,
                                     c.ClassReference,
                                     CASE WHEN s.PricingMode = 2 THEN 1 ELSE 0 END AS EnableHourlyRate
                                 FROM JustGoBookingClassSession AS s
                                 INNER JOIN JustGoBookingClass AS c ON c.ClassId = s.ClassId
                                 LEFT JOIN JustGoBookingAgeGroup AS ag ON ag.Id = s.AgeGroupId
                                 LEFT JOIN JustGoBookingClassColorGroup AS ccg ON ccg.ColorGroupId = s.ColorGroupId
                                 WHERE s.SessionId = @SessionId;

                                 SELECT *
                                 FROM JustGoBookingClassSessionSchedule
                                 WHERE SessionId = @SessionId;

                                 SELECT 
                                     SessionPriceOptionId,
                                     SessionId,
                                     SessionOptionId,
                                     PriceOption,
                                     Price,
                                     ApplyProRataDiscount,
                                     UnitPricePerSession,
                                     IsDynamicPrice,
                                     IsEnable
                                 FROM JustGoBookingClassSessionPriceOption
                                 WHERE SessionId = @SessionId;
                                 """;

            var parameters = new DynamicParameters();
            parameters.Add("@SessionGuid", sessionGuid, DbType.Guid);

            await using var multiResult = await repo.GetMultipleQueryAsync(query, cancellationToken, parameters,null,QueryType.Text);

            var session = await multiResult.ReadSingleOrDefaultAsync<JustGoBookingClassSessionInfoCalendarView>()
                          ?? new JustGoBookingClassSessionInfoCalendarView();

            session.SessionSchedules = (await multiResult.ReadAsync<JustGoBookingClassSessionSchedule>()).ToList();
            session.SessionPriceOptions =
                (await multiResult.ReadAsync<JustGoBookingClassSessionPriceOption>()).ToList();

            return session;
        }

        private async Task<List<AttendeeCalendarDto>> BuildAttendeeCalendarDtos(
            object attendeesObj,
            List<AttendeeStatus> eventAttendances,
            string classGuid,
            CancellationToken cancellationToken)
        {
            var attendeeCalendarDtos = new List<AttendeeCalendarDto>();
            var memberNotesRepo = _readRepository.GetLazyRepository<MemberNotesDto>().Value;

            var attendees = ((IEnumerable<dynamic>)attendeesObj).ToList();
            var memberDocIds = attendees.Select(a => (int)a.Entityid).Distinct().ToList();
            var memberNotesLookup = await GetMemberNotesByMemberDocId(memberDocIds, classGuid, memberNotesRepo, cancellationToken);

            foreach (var attendee in attendees)
            {
                var attendeeSchedules = new List<AttendeeScheduleDto>();

                foreach (var attendeeStatus in attendee.AttendeeStatusList)
                {
                    var attendance = eventAttendances.FirstOrDefault(ea =>
                        ea.CourseBookingDocId == (int)attendee.Coursebookingdocid &&
                        ea.ScheduleTicketRowId == (int)attendeeStatus.ScheduleTicketRowId);

                    string startDateStr = attendeeStatus.StartDate switch
                    {
                        DateTime dt => dt.ToString(FORMAT_DATE),
                        string s => s,
                        _ => attendeeStatus.StartDate?.ToString() ?? string.Empty
                    };

                    bool isTrial = attendeeStatus.IsTrial switch
                    {
                        int i => i == 1,
                        bool b => b,
                        string s => s == "1",
                        _ => false
                    };
                    if (attendee.Coursebookingdocid == 2631)
                    {
                        int i = 1;
                    }
                    string? cancellationJson = attendeeStatus.CancellationOrChangeLog;

                    attendeeSchedules.Add(new AttendeeScheduleDto
                    {
                        Id = attendance?.Id ?? -1,
                        Coursebookingdocid = (int)attendee.Coursebookingdocid,
                        ScheduleTicketRowId = (int)attendeeStatus.ScheduleTicketRowId,
                        StartDate = Convert.ToDateTime(startDateStr),
                        AttandanceDate = attendance?.AttandanceDate ?? DateTime.MinValue,
                        status = attendance?.AttendanceStatus ?? string.Empty,
                        IsTrial = isTrial,
                        AttendeeDetailsStatus = (int)attendeeStatus.AttendeeDetailsStatus,
                        AttendeeType = (int)attendeeStatus.AttendeeType,
                        date = attendeeStatus.Date?.ToString() ?? string.Empty,
                        CancellationOrChangeLog = !string.IsNullOrWhiteSpace(cancellationJson)
                            ? JsonConvert.DeserializeObject<CancellationOrChangeLogDto>(cancellationJson)
                            : null,
                        attendanceNotes = attendance?.AttendanceNotes?.Select(an => new AttendanceNote
                        {
                            Id = an.Id,
                            AttendanceId = an.AttendanceId,
                            Note = an.Note,
                        }).ToList() ?? new List<AttendanceNote>()
                    });
                }

                var memberDocId = (int)attendee.Entityid;
                memberNotesLookup.TryGetValue(memberDocId, out var memberNotes);

                var attendeeCalendarDto = new AttendeeCalendarDto
                {
                    Coursebookingdocid = (int)attendee.Coursebookingdocid,
                    TicketDocId = attendee.TicketDocId?.ToString() ?? string.Empty,
                    RowId = (int)attendee.RowId,
                    Entityid = memberDocId,
                    MemberName = attendee.MemberName?.ToString() ?? string.Empty,
                    ImageUrl = attendee.ImageUrl?.ToString() ?? string.Empty,
                    HasPhotoConsent = (bool)attendee.HasPhotoConsent,
                    ParentFirstname = attendee.ParentFirstname?.ToString() ?? string.Empty,
                    ParentLastname = attendee.ParentLastname?.ToString() ?? string.Empty,
                    ParentEmailAddress = attendee.ParentEmailAddress?.ToString() ?? string.Empty,
                    Age = attendee.Age?.ToString() ?? string.Empty,
                    MemberId = attendee.MemberId?.ToString() ?? string.Empty,
                    UserSyncId = attendee.UserSyncId?.ToString() ?? string.Empty,
                    FirstName = attendee.FirstName?.ToString() ?? string.Empty,
                    LastName = attendee.LastName?.ToString() ?? string.Empty,
                    EmailAddress = attendee.EmailAddress?.ToString() ?? string.Empty,
                    AttendeeStatusList = attendeeSchedules.OrderBy(s => s.StartDate).ToList(),
                    HasTrial = attendeeSchedules.Any(s => s.IsTrial),
                    MemberNotes = memberNotes ?? new MemberNotesDto()
                };
                attendeeCalendarDtos.Add(attendeeCalendarDto);
            }
            return attendeeCalendarDtos;
        }
        private async Task<List<ScheduleOccurrenceDto>> GenerateScheduleOccurrences(
            List<JustGoBookingClassSessionSchedule> sessionSchedules,
            string occurrenceIds,
            CancellationToken cancellationToken)
        {
            var listOfDates = new List<ScheduleOccurrenceDto>();
            var occurrenceRepo = _readRepository.GetLazyRepository<JustGoBookingScheduleOccurrence>().Value;

            foreach (var schedule in sessionSchedules)
            {
                var occurrences = await JustGoBookingScheduleOccurrenceByScheduleId(
                    schedule.SessionScheduleId,
                    occurrenceIds,
                    occurrenceRepo,
                    cancellationToken);

                foreach (var occurrence in occurrences)
                {
                    listOfDates.Add(new ScheduleOccurrenceDto
                    {
                        RowId = occurrence.OccurrenceId,
                        TicketDocId = -1,
                        ScheduleDateWithDay = occurrence.StartDate.ToString("ddd dd MMM yy"),
                        StartDate = occurrence.StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        EndDate = occurrence.EndDate.ToString("yyyy-MM-dd HH:mm:ss"),
                        IsHoliday = occurrence.AdditionalHolidayId > 0,
                        HolidayName = occurrence.HolidayName
                    });
                }
            }

            return listOfDates;
        }

        private static List<AttendeeCalendarDto> BuildCombinedResult(
            List<AttendeeCalendarDto> attendeeCalendarDtos,
            List<AttendeeChipStatusDto> attendeeChipStatuses,
            List<MemberEmergencyContact> contacts)
        {
            return attendeeCalendarDtos
                .GroupBy(d => d.Entityid)
                .Select(g =>
                {
                    var first = g.First();
                    var chipStatus = attendeeChipStatuses.FirstOrDefault(a => a.CourseBookingId == first.Coursebookingdocid);

                    return new AttendeeCalendarDto
                    {
                        Entityid = g.Key,
                        Coursebookingdocid = first.Coursebookingdocid,
                        ChipStatus = chipStatus?.ChipStatus!,
                        IsTransfer = chipStatus?.IsTransfer ?? false,
                        IsCancelled = chipStatus?.IsCancelled ?? false,
                        TicketDocId = first.TicketDocId,
                        RowId = first.RowId,
                        MemberName = first.MemberName,
                        ImageUrl = first.ImageUrl,
                        HasPhotoConsent = first.HasPhotoConsent,
                        ParentFirstname = first.ParentFirstname,
                        ParentLastname = first.ParentLastname,
                        ParentEmailAddress = first.ParentEmailAddress,
                        Age = first.Age,
                        MemberId = first.MemberId,
                        UserSyncId = first.UserSyncId,
                        HasTrial = first.HasTrial,
                        FirstName = first.FirstName,
                        LastName = first.LastName,
                        EmailAddress = first.EmailAddress,
                        MemberNotes = first.MemberNotes,
                        AttendeeStatusList = g.SelectMany(d => d.AttendeeStatusList)
                            .OrderBy(asl => asl.StartDate)
                            .ToList(),
                        EmergencyContacts = contacts.Where(c => c.MemberDocId == g.Key).ToList()
                    };
                })
                .ToList();
        }
        public async Task<(object Attendees,
            List<ListofDate> Dates, 
            int TotalCount)> GetAttendanceList(int sessionId, 
            string occurrenceIds,
            int pageNo,
            int pageSize,
            IReadRepository<object> repo, CancellationToken cancellationToken, string filterValue = "",
            bool isActiveMemberOnly = true)
        {
            var selectedattendees = new List<Attendee>();
            var listofDates = new List<ListofDate>();
            var listofAttendeeStatus = new List<AttendeeStatusDto>();
            var totalCount = 0;

            var sanitizeFilter = filterValue.ToLower().Replace("'", newValue: "''");
            var query = @"GetClassSessionAttendeeList_V2";

            var parameters = new DynamicParameters();
            parameters.Add("@SessionId", sessionId, DbType.Int32);
            parameters.Add("@OccurrenceIds", occurrenceIds, DbType.String);
            parameters.Add("@PageNumber", pageNo, DbType.Int32);
            parameters.Add("@RowsPerPage", pageSize, DbType.Int32);
            parameters.Add("@FilterValue", sanitizeFilter, DbType.String);
            parameters.Add("@IsActiveMemberOnly", isActiveMemberOnly, DbType.Boolean);
            await using var multiResult = await repo.GetMultipleQueryAsync(
                query,
                cancellationToken,
                parameters);

            // Result set #1: Attendees (may be empty)
            var attendeeResult = await multiResult.ReadAsync<Attendee>();
            selectedattendees = attendeeResult?.ToList() ?? [];

            // Result set #2: List of Dates (may be empty)
            var datesResult = await multiResult.ReadAsync<ListofDate>();
            listofDates = datesResult?.ToList() ?? [];

            // Result set #3: Attendee Status (may be empty)
            var statusResult = await multiResult.ReadAsync<AttendeeStatusDto>();
            listofAttendeeStatus = statusResult?.ToList() ?? [];
            // Result set #4: Total Count (should always exist)
            var countResult = await multiResult.ReadAsync<int>();
            totalCount = countResult?.FirstOrDefault() ?? 0;
            //totalCount = selectedattendees.Count;
            

            var attendeeList = selectedattendees.Select(x => new
            {
                x.Coursebookingdocid,
                x.TicketDocId,
                x.RowId,
                x.Entityid,
                x.MemberName,
                x.ImageUrl,
                x.HasPhotoConsent,
                x.ParentFirstname,
                x.ParentLastname,
                x.ParentEmailAddress,
                x.Age,
                x.UserSyncId,
                x.MemberId,
                x.FirstName,
                x.LastName,
                x.EmailAddress,
                AttendeeStatusList = listofAttendeeStatus
                    .Where(status => status.CourseBookingid == x.Coursebookingdocid).ToList()
            }).ToList();

            return (Attendees: attendeeList, Dates: listofDates, totalCount);
        }

        public async Task<List<AttendeeStatus>> GetEventAttendanceAndNotes(
            int sessionId,
            string occurrenceIds,
            IReadRepository<AttendeeStatus> repo,
            CancellationToken cancellationToken,
            int? entityDocId = null)
        {
            var query = """
                        SELECT 
                            ad.AttendeeDetailsId AS Id,  
                            ba.AttendeeId AS CourseBookingDocId,
                            so.OccurrenceId AS ScheduleTicketRowId,
                            ISNULL(ad.Status, '') AS AttendanceStatus,
                            so.StartDate AS AttendanceDate,
                            '' AS Note,
                            ad.AttendeeType     
                        FROM JustGoBookingAttendee ba
                        INNER JOIN JustGoBookingAttendeeDetails ad
                            ON ba.AttendeeId = ad.AttendeeId
                        INNER JOIN JustGoBookingScheduleOccurrence so
                            ON ad.OccurenceId = so.OccurrenceId
                        INNER JOIN STRING_SPLIT(@OccurrenceIds, ',') s
                            ON so.OccurrenceId = TRY_CAST(LTRIM(RTRIM(s.value)) AS INT)
                        WHERE ba.SessionId = @SessionId
                          AND TRY_CAST(LTRIM(RTRIM(s.value)) AS INT) IS NOT NULL;
                        """;

            if (entityDocId.HasValue)
            {
                query += " AND ba.EntityDocId = @EntityDocId";
            }

            var parameters = new DynamicParameters();
            parameters.Add("@sessionId", sessionId, DbType.Int32);
            parameters.Add("@OccurrenceIds", occurrenceIds, DbType.String);

            if (entityDocId.HasValue)
            {
                parameters.Add("@entityDocId", entityDocId.Value, DbType.Int32);
            }

            // Get attendances
            var attendances = (await repo.GetListAsync(query, cancellationToken, parameters, null, "text")).ToList();

            if (attendances.Count == 0)
            {
                return attendances;
            }


            var attendanceIds = attendances.Select(a => a.Id).Distinct().ToList();
            var noteRepo = _readRepository.GetLazyRepository<AttendanceNote>().Value;

            const int maxSqlParameters = 2000;
            var attendanceNotes = new List<AttendanceNote>();

            for (int start = 0; start < attendanceIds.Count; start += maxSqlParameters)
            {
                var batch = attendanceIds
                    .Skip(start)
                    .Take(maxSqlParameters)
                    .ToList();

                var noteParameters = new DynamicParameters();
                var inClauseParams = new List<string>();

                for (int i = 0; i < batch.Count; i++)
                {
                    var paramName = $"@Id{i}";
                    inClauseParams.Add(paramName);
                    noteParameters.Add(paramName, batch[i], DbType.Int32);
                }

                var noteQuery = $"""
                                 SELECT 
                                     AttendeeDetailNoteId AS Id,
                                     AttendeeDetailsId AS AttendanceId,
                                     Note,
                                     CreatedDate,
                                     ModifiedDate
                                 FROM JustGoBookingAttendeeDetailNote
                                 WHERE AttendeeDetailsId IN ({string.Join(",", inClauseParams)})
                                 """;

                var batchNotes = await noteRepo.GetListAsync(noteQuery, cancellationToken, noteParameters, null, "text");
                attendanceNotes.AddRange(batchNotes);
            }
            // Group notes by AttendanceId for efficient lookup
            var notesByAttendanceId = attendanceNotes
                .GroupBy(n => n.AttendanceId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Map notes to attendances
            foreach (var attendance in attendances)
            {
                attendance.AttendanceNotes = notesByAttendanceId.TryGetValue(attendance.Id, out var notes)
                    ? notes
                    : [];
            }

            return attendances;
        }

        public async Task<List<AttendeeChipStatusDto>> AttendeeChipStatusBySessionId(int sessionId, string occurrenceIds, int? entityDocId,
            IReadRepository<object> repo, CancellationToken cancellationToken)
        {
            string query = @"AttendeeChipStatusBySessionID_V2";
            var parameters = new DynamicParameters();
            parameters.Add("@sessionId", sessionId, DbType.Int32);
            parameters.Add("@OccurrenceIds", occurrenceIds, DbType.String);

            if (entityDocId.HasValue)
            {
                parameters.Add("@entityDocId", entityDocId.Value, DbType.Int32);
            }


            var attendeeChipStatus =
                (await repo.GetListAsync<AttendeeChipStatusDto>(query, parameters, null, QueryType.StoredProcedure,
                    cancellationToken)).ToList();
            return attendeeChipStatus;

        }

        public async Task<Dictionary<int, MemberNotesDto>> GetMemberNotesByMemberDocId(
            IReadOnlyCollection<int> memberDocIds,
            string classGuid,
            IReadRepository<MemberNotesDto> repo,
            CancellationToken cancellationToken)
        {
            string query = @"
                            DECLARE @OwnerId INT = 0;
                            
                            set @OwnerId = (select OwningEntityId from JustGoBookingClass where ClassGuid = @ClassGuid);
                            
                            WITH MemberList AS (
                                SELECT DISTINCT TRY_CAST(value AS INT) AS MemberDocId
                                FROM STRING_SPLIT(@MemberDocIds, ',')
                                WHERE TRY_CAST(value AS INT) IS NOT NULL
                            )
                            SELECT 
                                ml.MemberDocId,
                                CASE WHEN COALESCE(SUM(CASE WHEN nc.NoteCategoryId = 2 THEN 1 ELSE 0 END), 0) > 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasMedicalNote,
                                CASE WHEN COALESCE(SUM(CASE WHEN nc.NoteCategoryId = 3 THEN 1 ELSE 0 END), 0) > 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasAlertNote,
                                CASE WHEN COALESCE(COUNT(DISTINCT mn.NoteCategoryId), 0) > 1 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasMultipleNotes,
                                CASE 
                                    WHEN COALESCE(COUNT(DISTINCT mn.NoteCategoryId), 0) > 1 THEN 'Multiple notes, click to see details'
                                    ELSE COALESCE(MAX(mn.Details), '')
                                END AS Details,
                                COALESCE(COUNT(mn.NotesId), 0) AS TotalCount
                            FROM MemberList ml
                            LEFT JOIN [User] u ON u.MemberDocId = ml.MemberDocId
                            INNER JOIN MemberNotes mn ON mn.EntityID = u.UserId
                                AND mn.OwnerId = @OwnerId
                                AND mn.IsActive = 1
                                AND mn.IsHide = 0
                                AND mn.NoteCategoryId <> 1
                            INNER JOIN NoteCategories nc ON nc.NoteCategoryId = mn.NoteCategoryId
                                AND nc.IsActive = 1
                            GROUP BY ml.MemberDocId;";

            var parameters = new DynamicParameters();
            parameters.Add("@MemberDocIds", string.Join(",", memberDocIds), DbType.String);
            parameters.Add("@ClassGuid", classGuid, DbType.String);

            var noteResults = (await repo.GetListAsync<MemberNotesDto>(
                query,
                parameters,
                null,
                QueryType.Text,
                cancellationToken)).ToList();

            return noteResults.ToDictionary(
                result => result.MemberDocId,
                result => new MemberNotesDto
                {
                    HasMedicalNote = result.HasMedicalNote,
                    HasAlertNote = result.HasAlertNote,
                    HasMultipleNotes = result.HasMultipleNotes,
                    Details = result.Details,
                    TotalCount = result.TotalCount
                });
        }

        public async Task<List<JustGoBookingScheduleOccurrence>> JustGoBookingScheduleOccurrenceByScheduleId(
            int scheduleId,string occurrenceIds, IReadRepository<JustGoBookingScheduleOccurrence> repo, CancellationToken cancellationToken)
        {
            string query = """
                           DECLARE @TermRollingPeriodId INT;
                           DECLARE @TermType INT;
                           DECLARE @StopRolling BIT;
                           DECLARE @RollingDurationInMonths INT;
                           DECLARE @TermStartDate DATETIME;
                           DECLARE @TermEndDate DATETIME;
                           DECLARE @UtcNow datetime = GETUTCDATE()
                           --DECLARE @TodayDate date = CAST(@UtcNow AS DATE)

                           -- get term metadata (including term start/end)
                           SELECT DISTINCT 
                               @TermRollingPeriodId = ct.TermRollingPeriodId,
                               @TermType = ct.TermType,
                               @StopRolling = ct.StopRolling,
                               @TermStartDate = ct.StartDate,
                               @TermEndDate = ct.EndDate
                           FROM JustGoBookingScheduleOccurrence so 
                           INNER JOIN JustGoBookingClassSessionSchedule css 
                               ON so.ScheduleId = css.SessionScheduleId
                               AND (
                               so.StartDate < @UtcNow  -- Past: schedule deleted will show
                               OR (so.StartDate >= @UtcNow AND ISNULL(css.IsDeleted, 0) = 0)  -- Future: schedule need to be active
                           )
                           INNER JOIN JustGoBookingClassSession cs 
                               ON css.SessionId = cs.SessionId AND cs.IsDeleted = 0
                           INNER JOIN JustGoBookingClassTerm ct 
                               ON cs.TermId = ct.ClassTermId
                           WHERE so.ScheduleId = @ScheduleId AND (
                               so.StartDate < @UtcNow
                               OR so.StartDate >= @UtcNow AND ISNULL(so.IsDeleted, 0) = 0)
                          
                           --WHERE so.IsDeleted = 0 AND so.ScheduleId = @ScheduleId;

                           IF @TermType = 1 AND @TermRollingPeriodId IS NOT NULL AND @StopRolling = 0
                           BEGIN
                               SELECT @RollingDurationInMonths = RollingDurationInMonths
                               FROM JustGoBookingClassTermRollingPeriod
                               WHERE TermRollingPeriodId = @TermRollingPeriodId;
                           END

                           DECLARE @WindowStart DATE = @TermStartDate;
                           DECLARE @WindowEnd   DATE = CASE 
                                                         WHEN @RollingDurationInMonths IS NOT NULL 
                                                           THEN DATEADD(MONTH, @RollingDurationInMonths, CAST(GETDATE() AS DATE))
                                                         ELSE CAST(GETDATE() AS DATE)
                                                       END;

                           IF (@TermRollingPeriodId IS NOT NULL
                               AND @TermType = 1
                               AND @StopRolling = 0
                               AND @TermStartDate IS NOT NULL
                               AND @TermEndDate IS NOT NULL)
                           BEGIN
                               SET @WindowEnd   = CAST(@TermEndDate   AS DATE);
                           END

                           SELECT 
                               O.OccurrenceId, 
                               O.ScheduleId, 
                               O.EntityTypeId, 
                               O.OwnerId, 
                               O.StartDate, 
                               O.EndDate, 
                               ISNULL(AH.AdditionalHolidayId, 0) AdditionalHolidayId, 
                               AH.[Name] HolidayName
                           FROM JustGoBookingScheduleOccurrence O
                           LEFT JOIN JustGoBookingAdditionalHoliday AH 
                               ON AH.OccurrenceId = O.OccurrenceId and isnull(AH.IsDeleted, 0) = 0
                           WHERE O.ScheduleId = @ScheduleId
                                AND O.OccurrenceId IN (
                                SELECT CAST(value AS INT) 
                                FROM STRING_SPLIT(@OccurrenceIds, ',')
                           )
                           AND (
                               O.StartDate < @UtcNow
                               OR (O.StartDate >= @UtcNow AND ISNULL(O.IsDeleted, 0) = 0)
                           )
                             AND (
                                   (@TermType = 0 OR @TermRollingPeriodId IS NULL OR @StopRolling = 1 OR @RollingDurationInMonths IS NULL)
                                   OR
                                   (CAST(O.EndDate AS DATE) >= @WindowStart AND CAST(O.EndDate AS DATE) <= @WindowEnd)
                                 );
                           """;
            var parameters = new DynamicParameters();
            parameters.Add("@scheduleId", scheduleId, DbType.Int32);
            parameters.Add("@OccurrenceIds", occurrenceIds, DbType.String);

            var scheduleOccurrences =
                (await repo.GetListAsync<JustGoBookingScheduleOccurrence>(query, parameters, null, QueryType.Text, cancellationToken)).ToList();
            return scheduleOccurrences;
        }

        public async Task<List<MemberEmergencyContact>> GetEmergencyContacts(string memberIds,
            IReadRepository<MemberEmergencyContact> repo, CancellationToken cancellationToken)
        {
            string query = $@"
                        SELECT DocId MemberDocId, CONCAT(ISNULL(FirstName, ''), ' ', ISNULL(SurName, '')) [Name], Relation, ContactNumber, EmailAddress
                        FROM Members_EmergencyContact
                        WHERE DocId IN (SELECT Value FROM string_split('{memberIds}', ','))";
            var parameters = new DynamicParameters();
            parameters.Add("@memberIds", memberIds, DbType.String);
            var emergencyContacts = await repo.GetListAsync<MemberEmergencyContact>(query, parameters, null, QueryType.Text, cancellationToken);
            return emergencyContacts.ToList();
        }
        //public async Task<int> ResolveSessionGuidToId(Guid sessionGuid, CancellationToken cancellationToken = default)
        //{
        //    var parameters = new DynamicParameters();
        //    parameters.Add("@SessionGuid", sessionGuid);
        //    const string sql = "select SessionId as SessionId from justgobookingclasssession where ClassSessionGuid = @SessionGuid";

        //    var result = await readRepository
        //        .GetLazyRepository<object>()
        //        .Value
        //        .GetSingleAsync<int>(sql, parameters, null, cancellationToken, "text");
        //    return result;
        //}
    }
}
