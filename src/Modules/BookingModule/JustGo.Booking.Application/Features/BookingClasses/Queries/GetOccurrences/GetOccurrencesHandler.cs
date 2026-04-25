using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Booking.Domain.Entities;
using MapsterMapper;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetOccurrences
{
    public class GetOccurrencesHandler : IRequestHandler<GetOccurrencesQuery, List<BookingOccurrenceDto>>
    {
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IMapper _mapper;

        public GetOccurrencesHandler(IReadRepositoryFactory readRepository, IMapper mapper)
        {
            _readRepository = readRepository;
            _mapper = mapper;
        }

        public async Task<List<BookingOccurrenceDto>> Handle(GetOccurrencesQuery request, CancellationToken cancellationToken)
        {
            var data = await GetOccurrencesAsync(request, cancellationToken);
            return _mapper.Map<List<BookingOccurrenceDto>>(data);
        }

        private async Task<List<BookingOccurrence>> GetOccurrencesAsync(GetOccurrencesQuery request, CancellationToken cancellationToken)
        {

            var sql = $"""
                       DECLARE @SessionId INT;
                       DECLARE @TermType BIT = NULL;
                       DECLARE @StopRolling BIT = NULL;
                       DECLARE @TermRollingPeriodId INT = NULL;
                       DECLARE @RollingDurationInMonths INT = NULL;
                       DECLARE @TermStartDate DATETIME = NULL;
                       DECLARE @TermEndDate DATETIME = NULL;
                       DECLARE @IsTrial BIT = 0;
                       DECLARE @IsPayg BIT = 0;
                       DECLARE @TrialProductId INT = -1;
                       DECLARE @CurrentDate DATE = GETUTCDATE();
                       DECLARE @UtcNow datetime = GETUTCDATE()
                       --DECLARE @TodayDate date = CAST(@UtcNow AS DATE)

                       SET @SessionId = (SELECT TOP 1 SessionId FROM JustGoBookingClassSession WHERE ClassSessionGuid = @SessionGuid);

                       SELECT TOP 1
                       @TrialProductId = p.ProductId,
                       @IsTrial   = ISNULL(o.IsTrial, 0)
                       FROM dbo.JustGoBookingClassSessionProduct AS p
                       INNER JOIN dbo.JustGoBookingClassSessionOption AS o
                       ON p.SessionId = o.SessionId
                       WHERE p.SessionId = @SessionId
                       AND p.ProductType = 2 -- trial product
                       AND ISNULL(p.IsDeleted, 0) = 0
                       AND ISNULL(o.IsDeleted, 0) = 0;

                       SELECT TOP 1
                       @TermType = T.TermType,
                       @StopRolling = T.StopRolling,
                       @TermRollingPeriodId = T.TermRollingPeriodId,
                       @TermStartDate = T.StartDate,
                       @TermEndDate = T.EndDate
                       FROM JustGoBookingClassSession S
                       INNER JOIN JustGoBookingClassTerm T ON S.TermId = T.ClassTermId
                       WHERE S.SessionId = @SessionId
                       AND ISNULL(S.IsDeleted, 0) = 0
                       AND ISNULL(T.IsDeleted, 0) = 0;


                       IF (@TermType = 1 AND @TermRollingPeriodId IS NOT NULL)
                       BEGIN
                       SELECT @RollingDurationInMonths = RP.RollingDurationInMonths
                       FROM JustGoBookingClassTermRollingPeriod RP
                       WHERE RP.TermRollingPeriodId = @TermRollingPeriodId;
                       END

                       DECLARE @WindowStart DATE;
                       DECLARE @WindowEnd DATE;

                       SET @WindowStart = CAST(@TermStartDate AS DATE);
                       SET @WindowEnd = '2099-12-31';

                       IF (@TermType = 1 AND ISNULL(@StopRolling, 0) = 0 AND @RollingDurationInMonths IS NOT NULL)
                       BEGIN
                       -- future
                       IF (@TermStartDate IS NOT NULL AND CAST(@TermStartDate AS DATE) > CAST(@CurrentDate AS DATE))
                       BEGIN
                       SET @WindowEnd = DATEADD(MONTH, @RollingDurationInMonths, CAST(@TermStartDate AS DATE));
                       END
                       ELSE
                       BEGIN
                       SET @WindowEnd = DATEADD(MONTH, @RollingDurationInMonths, CAST(@CurrentDate AS DATE));
                       END
                       END
                       ELSE IF (@TermType = 0)
                       BEGIN
                       SET @WindowEnd = @TermEndDate;
                       END;

                       ;WITH TrialReservations AS (
                       SELECT 
                       SessionId,
                       BookedQty,
                       OccurrenceId,
                       BookedDate AS BookedDateOnly,
                       ReservedEndDate
                       FROM dbo.fn_GetTrialReserveQtyBySession(@IsPayg, @SessionId, @TrialProductId, NULL)
                       WHERE 1 = CASE 
                       WHEN @IsTrial = 1 AND @TrialProductId IS NOT NULL AND @TrialProductId <> -1 
                       THEN 1 
                       ELSE 0 
                       END
                       ),
                       TrialReserveSummary AS (
                       SELECT 
                       SessionId,
                       SUM(BookedQty) AS TotalReserveQty,
                       MAX(ReservedEndDate) AS MaxReservedEndDate
                       FROM TrialReservations
                       GROUP BY SessionId
                       ),
                       BookedDates AS (
                       SELECT DISTINCT 
                       a.SessionId,
                       o.StartDate  AS BookedDateOnly
                       FROM dbo.JustGoBookingAttendee AS a
                       INNER JOIN dbo.JustGoBookingAttendeeDetails AS ad
                       ON ad.AttendeeId = a.AttendeeId
                       AND ad.AttendeeDetailsStatus = 1
                       INNER JOIN dbo.JustGoBookingScheduleOccurrence AS o
                       ON o.OccurrenceId = ad.OccurenceId
                       AND ISNULL(o.IsDeleted, 0) = 0
                       WHERE a.SessionId = @SessionId
                       AND a.[Status] = 2  -- trial booking
                       ),
                       OCCRS AS (
                       SELECT 
                       S.SessionId,
                       S.Capacity,
                       OCR.OccurrenceId,
                       OCR.ScheduleId,
                       OCR.StartDate,
                       OCR.EndDate,
                       OPT.TrialBookingPeriod,
                       OPT.IsTrial,
                       OPT.TrialExpiryPeriod,
                       SIGN(ISNULL(AH.SessionId, 0)) IsHoliday
                       FROM JustGoBookingScheduleOccurrence OCR
                       INNER JOIN JustGoBookingClassSessionSchedule SC 
                       ON SC.SessionScheduleId = OCR.ScheduleId 
                       --AND ISNULL(SC.IsDeleted, 0) = 0
                       AND (
                       OCR.StartDate < @UtcNow
                       OR (OCR.StartDate >= @UtcNow AND ISNULL(SC.IsDeleted, 0) = 0)
                       )

                       INNER JOIN JustGoBookingClassSession S 
                       ON S.SessionId = SC.SessionId 
                       AND ISNULL(S.IsDeleted, 0) = 0
                       INNER JOIN JustGoBookingClassSessionOption OPT 
                       ON OPT.SessionId = S.SessionId 
                       AND ISNULL(OPT.IsDeleted, 0) = 0
                       LEFT JOIN JustGoBookingAdditionalHoliday AH 
                       ON AH.SessionId = S.SessionId 
                       AND AH.OccurrenceId = OCR.OccurrenceId
                       AND ISNULL(AH.IsDeleted, 0) = 0 
                       WHERE S.ClassSessionGuid = @SessionGuid
                       --AND ISNULL(OCR.IsDeleted, 0) = 0
                       AND (
                       OCR.StartDate < @UtcNow
                       OR (OCR.StartDate >= @UtcNow AND ISNULL(OCR.IsDeleted, 0) = 0)
                       )
                       AND CAST(OCR.StartDate AS DATE) BETWEEN @WindowStart AND @WindowEnd
                       ),
                       Trialable AS (
                       SELECT SessionId, OccurrenceId, TrialExpiryPeriod
                       FROM (
                       SELECT 
                       SessionId, 
                       OccurrenceId, 
                       TrialExpiryPeriod,
                       ROW_NUMBER() OVER (ORDER BY StartDate ASC) AS TrialRowNum,
                       TrialBookingPeriod
                       FROM OCCRS
                       WHERE EndDate >= @CurrentDate 
                       AND IsTrial = 1 
                       AND IsHoliday = 0
                       ) T 
                       WHERE TrialRowNum <= TrialBookingPeriod
                       ),
                       OccrInfo AS (
                       SELECT SessionId, OccurrenceId, AvailableQty, BookedQty,PaygCount
                       FROM dbo.GetOccurenceBookingDetails(@SessionId, '')
                       ),
                       PaygSpread AS (
                       SELECT 
                       M.OccurrenceId,
                       COUNT(X.StartDate) AS PaygSpreadCount
                       FROM OCCRS M
                       LEFT JOIN (
                       SELECT PB.SessionId, PB.OccurenceId, PAYG_OCC.StartDate
                       FROM (
                       SELECT A.SessionId, AD.OccurenceId
                       FROM dbo.JustGoBookingAttendee A
                       INNER JOIN dbo.JustGoBookingAttendeeDetails AD 
                       ON AD.AttendeeId = A.AttendeeId
                       AND AD.AttendeeDetailsStatus = 1
                       WHERE A.SessionId = @SessionId
                       AND A.[Status] = 3

                       UNION ALL

                       -- Trial upgraded to PAYG
                       SELECT A.SessionId, AD.OccurenceId
                       FROM dbo.JustGoBookingAttendee A
                       INNER JOIN dbo.JustGoBookingAttendeeDetails AD 
                       ON AD.AttendeeId = A.AttendeeId
                       AND AD.AttendeeDetailsStatus = 1
                       WHERE A.SessionId = @SessionId
                       AND A.[Status] = 2
                       AND AD.AttendeeType = 3
                       ) PB
                       INNER JOIN OCCRS PAYG_OCC
                       ON PAYG_OCC.OccurrenceId = PB.OccurenceId
                       ) X
                       ON X.StartDate >= M.StartDate
                       GROUP BY M.OccurrenceId
                       ),
                       TrialCountPerOccurrence AS (
                       SELECT 
                       OCR.OccurrenceId,
                       COUNT(DISTINCT a.EntityDocId) AS TrialCount
                       FROM dbo.JustGoBookingAttendee a
                       INNER JOIN dbo.JustGoBookingAttendeeDetails ad 
                       ON ad.AttendeeId = a.AttendeeId 
                       AND ad.AttendeeDetailsStatus = 1
                       INNER JOIN dbo.JustGoBookingScheduleOccurrence OCR
                       ON OCR.OccurrenceId = ad.OccurenceId
                       WHERE a.SessionId = @SessionId
                       AND a.[Status] = 2  -- Trial bookings only
                       GROUP BY OCR.OccurrenceId
                       ),
                       AllActiveTrials AS (
                       SELECT 
                       a.SessionId,
                       COUNT(DISTINCT a.EntityDocId) AS TotalTrialHolders
                       FROM dbo.JustGoBookingAttendee a
                       INNER JOIN dbo.JustGoBookingAttendeeDetails ad 
                       ON ad.AttendeeId = a.AttendeeId AND ad.AttendeeDetailsStatus = 1
                       WHERE a.SessionId = @SessionId
                       AND a.[Status] = 2
                       GROUP BY a.SessionId
                       )
                       SELECT 
                       O.SessionId, 
                       O.Capacity,
                       O.OccurrenceId,
                       O.ScheduleId,
                       O.StartDate,
                       O.EndDate,
                       O.IsHoliday,
                       SIGN(ISNULL(T.OccurrenceId, 0)) IsTrialable,
                       IIF(O.EndDate >= @UtcNow, 1, 0) IsFuture,
                       GREATEST(0,
                       (ISNULL(OI.BookedQty, 0) - ISNULL(OI.PaygCount, 0) + ISNULL(PS.PaygSpreadCount, 0))
                       + CASE 
                       WHEN TRS.MaxReservedEndDate IS NULL 
                       OR GETUTCDATE() > TRS.MaxReservedEndDate
                       THEN ISNULL(AAT.TotalTrialHolders, 0) - ISNULL(TCO.TrialCount, 0)
                       ELSE 0
                       END
                       ) AS BookedQty,
                       CASE 
                       WHEN TRS.MaxReservedEndDate IS NOT NULL 
                       AND GETUTCDATE() <= TRS.MaxReservedEndDate
                       AND O.StartDate >= GETUTCDATE()
                       AND BD.BookedDateOnly IS NULL
                       THEN TRS.TotalReserveQty
                       ELSE 0
                       END AS TrialReserveQty
                       FROM OCCRS O
                       LEFT JOIN Trialable T ON T.OccurrenceId = O.OccurrenceId
                       LEFT JOIN OccrInfo OI ON OI.OccurrenceId = O.OccurrenceId 
                       LEFT JOIN TrialReserveSummary TRS ON TRS.SessionId = O.SessionId
                       LEFT JOIN BookedDates BD ON BD.SessionId = O.SessionId AND BD.BookedDateOnly = O.StartDate
                       LEFT JOIN AllActiveTrials AAT ON AAT.SessionId = O.SessionId
                       LEFT JOIN TrialCountPerOccurrence TCO ON TCO.OccurrenceId = O.OccurrenceId
                       LEFT JOIN PaygSpread PS ON PS.OccurrenceId = O.OccurrenceId
                       ORDER BY O.StartDate;
                       """;

            return (await _readRepository.GetLazyRepository<BookingOccurrence>().Value.GetListAsync(sql, cancellationToken, new { SessionGuid = request.SessionGuid.ToString()}, null, "text")).ToList();
        }

    }
}
