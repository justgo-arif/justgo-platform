using Dapper;
using JustGo.Authentication.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Booking.Domain.Entities;
using MapsterMapper;
using System.Data;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.ClassDetails;

public class GetClassDetailsHandler : IRequestHandler<GetClassDetailsQuery, BookingClassDetailsDto?>
{
    private readonly IReadRepositoryFactory _readRepository;
    private readonly IHybridCacheService _cache;
    private readonly IMapper _mapper;
    private readonly IUtilityService _utilityService;
    public GetClassDetailsHandler(IReadRepositoryFactory readRepository,
        IHybridCacheService cache,
        IMapper mapper,
        IUtilityService utilityService)
    {
        _readRepository = readRepository;
        _cache = cache;
        _mapper = mapper;
        _utilityService = utilityService;
    }

    public async Task<BookingClassDetailsDto?> Handle(GetClassDetailsQuery request, CancellationToken cancellationToken)
    {
        int userId = -1;
        int memberId = -1;
        var currentUser = await _utilityService.GetCurrentUserPublic(cancellationToken);
        if (currentUser is not null)
        {
            userId = currentUser.UserId;
            memberId = currentUser.MemberDocId;
        }

        string cacheKey = $"justgobooking:class-details:{request.SessionGuid}";

        var result = await _cache.GetOrSetAsync(
            cacheKey,
            async _ =>
            {
                var details = await GetClassDetailsAsync(request, cancellationToken);
                return details is null ? null : MapToDto(details);
            },
            TimeSpan.FromMinutes(10),
            [nameof(CacheTag.Class)],
            cancellationToken
        );

        var bookingSession = await GetBookingSessionAsync(request, userId, memberId, cancellationToken);
        if (result?.Class != null && bookingSession != null)
        {
            result.Class.IsWaitable = GetWaitableStatus(bookingSession.WaitlistOnly, bookingSession.NoOfInvite, bookingSession.AllSessionsFull, bookingSession.AvailableFullBookQty);
        }

        return result;
    }

    private async Task<BookingClassDetails?> GetClassDetailsAsync(GetClassDetailsQuery request, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        queryParameters.Add("@ClassSessionGuid", request.SessionGuid.ToString(), DbType.String, size: 100);

        var sql = $"""
                   DECLARE @BaseUrl Varchar(100) = (SELECT [Value] FROM SystemSettings Where ItemKey = 'SYSTEM.SITEADDRESS')
                   SET @BaseUrl = IIF(RIGHT(@BaseUrl, 1) = '/', LEFT(@BaseUrl, LEN(@BaseUrl) - 1), @BaseUrl);
                   
                   DECLARE @SessionId INT,
                     @ClassId INT,
                     @TermId INT,
                     @TermType bit = null,
                     @StopRolling bit = null,
                     @TermRollingPeriodId INT = null,
                     @RollingDurationInMonths INT = null,
                     @TermStartDate Datetime = null,
                     @TermEndDate Datetime = null;
                   
                   SELECT TOP 1 @SessionId = S.SessionId,
                     @ClassId = S.ClassId,
                     @TermId = t.ClassTermId,
                     @TermType = t.TermType,
                     @StopRolling = t.StopRolling,
                     @TermRollingPeriodId = t.TermRollingPeriodId,
                     @TermStartDate = t.StartDate,
                     @TermEndDate = t.EndDate
                   FROM JustGoBookingClassSession S
                     INNER JOIN JustGoBookingClassTerm t on S.TermId = t.ClassTermId
                   WHERE S.ClassSessionGuid = @ClassSessionGuid and isnull(s.IsDeleted,0) = 0 and isnull(t.IsDeleted,0) = 0;
                   
                   
                   IF ( @TermType = 1 AND @TermRollingPeriodId IS NOT NULL) 
                       BEGIN
                           SELECT TOP 1 @RollingDurationInMonths = RP.RollingDurationInMonths
                           FROM JustGoBookingClassTermRollingPeriod AS RP
                           WHERE RP.TermRollingPeriodId = @TermRollingPeriodId
                       END
                   
                   DECLARE @WindowStart DATE = @TermStartDate;
                   DECLARE @WindowEnd DATE = @TermEndDate;
                   
                   IF (@TermType = 1 AND ISNULL(@StopRolling, 0) = 0 AND @RollingDurationInMonths IS NOT NULL)
                   BEGIN
                       IF (@TermStartDate IS NOT NULL AND CAST(@TermStartDate AS DATE) > CAST(GETDATE() AS DATE))
                       BEGIN
                           SET @WindowStart = CAST(@TermStartDate AS DATE);
                           SET @WindowEnd = ISNULL(CAST(@TermEndDate AS DATE), DATEADD(MONTH, @RollingDurationInMonths, CAST(@TermStartDate AS DATE)));
                       END
                       ELSE
                       BEGIN
                           SET @WindowStart = CAST(GETDATE() AS DATE);
                           SET @WindowEnd = DATEADD(MONTH, @RollingDurationInMonths, CAST(GETDATE() AS DATE));
                       END
                   END
                    
                   -- ===== PRO-RATA DISCOUNT CALCULATION =====
                   DECLARE @OneOffProductId INT = NULL;
                   DECLARE @OneOffProRataDiscount DECIMAL(10,2) = 0;
                   DECLARE @EntityDocId INT = 0;
                   
                   -- Get OneOff Product (ProductType = 1, PriceOption = 1)
                   SELECT TOP 1 @OneOffProductId = sp.ProductId
                   FROM dbo.JustGoBookingClassSessionProduct sp
                   INNER JOIN dbo.JustGoBookingClassSessionPriceOption spo ON spo.SessionId = sp.SessionId 
                       AND spo.SessionPriceOptionId = sp.SessionPriceOptionId
                   WHERE sp.SessionId = @SessionId 
                     AND ISNULL(sp.IsDeleted, 0) = 0
                     AND sp.ProductType = 1
                     AND spo.PriceOption = 1
                     AND spo.IsEnable = 1;
                     
                   DECLARE @proRataDiscounts TABLE (
                       proRataDiscount DECIMAL(10,2)
                   );
                   
                   -- Calculate OneOff ProRata discount
                   IF (@OneOffProductId IS NOT NULL)
                   BEGIN
                       INSERT INTO @proRataDiscounts (proRataDiscount)
                       EXEC dbo.CalculateProRataDiscountByClassProduct @OneOffProductId, @EntityDocId;
                   
                       SELECT @OneOffProRataDiscount = proRataDiscount FROM @proRataDiscounts;
                       DELETE FROM @proRataDiscounts;
                   END
                   -- ===== END PRO-RATA DISCOUNT CALCULATION =====
                   
                   ;WITH SCHEDULE AS (
                     SELECT CSS.SessionId,
                       STRING_AGG(
                         (
                           CSS.[DayOfWeek] + '|' + CAST(CSS.StartTime AS VARCHAR) + '|' + CAST(CSS.EndTime AS VARCHAR)
                         ),
                         ','
                       ) ScheduleInfo
                     FROM JustGoBookingClassSessionSchedule CSS
                     WHERE CSS.SessionId = @SessionId
                       AND CSS.IsDeleted != 1
                     GROUP BY CSS.SessionId
                   ),
                   IMGS AS (
                     SELECT A.ClassId,
                       STRING_AGG(
                         CONCAT(
                           @BaseUrl,
                           '/store/downloadpublic?f=',
                           A.[Name],
                           '&t=justgobookingattachment&p=',
                           A.EntityId,
                           '&p1=',
                           A.EntityTypeId
                         ),
                         '|'
                       ) ClassImages
                     FROM (
                         SELECT A.EntityId ClassId,A.[Name],A.EntityId,A.EntityTypeId
                         FROM JustGoBookingAttachment A
                         WHERE A.EntityId = @ClassId AND A.EntityTypeId = 1 AND A.IsDeleted != 1
                         GROUP BY A.EntityId,A.[Name],A.EntityId,A.EntityTypeId
                       ) A
                     GROUP BY A.ClassId
                   ),
                   OccurrenceAffectiveMonth AS (
                       SELECT @SessionId AS SessionId, MIN(OCR.StartDate) AS StartDate
                       FROM JustGoBookingClassSessionSchedule SC
                       INNER JOIN JustGoBookingScheduleOccurrence OCR ON OCR.ScheduleId = SC.SessionScheduleId 
                           AND ISNULL(OCR.IsDeleted, 0) = 0
                       LEFT JOIN JustGoBookingAdditionalHoliday AH ON AH.SessionId = SC.SessionId 
                           AND AH.OccurrenceId = OCR.OccurrenceId
                       WHERE SC.SessionId = @SessionId
                           AND ISNULL(SC.IsDeleted, 0) = 0
                           AND AH.SessionId IS NULL
                           AND OCR.EndDate >= GETUTCDATE()
                           AND CAST(OCR.StartDate AS DATE) >= @WindowStart
                           AND CAST(OCR.StartDate AS DATE) <= @WindowEnd
                   ),
                   NumberOfOccrnes AS (
                       SELECT @SessionId AS SessionId,COUNT(OCR.OccurrenceId) AS OccerencesCount
                       FROM JustGoBookingScheduleOccurrence OCR
                       INNER JOIN JustGoBookingClassSessionSchedule SC ON SC.SessionScheduleId = OCR.ScheduleId
                           AND SC.IsDeleted != 1
                           AND OCR.IsDeleted != 1
                           AND SC.SessionId = @SessionId
                       LEFT JOIN JustGoBookingAdditionalHoliday AH ON AH.SessionId = @SessionId
                           AND AH.OccurrenceId = OCR.OccurrenceId
                       CROSS APPLY (SELECT StartDate FROM OccurrenceAffectiveMonth) OAM
                       WHERE AH.SessionId IS NULL
                           AND MONTH(OCR.StartDate) = MONTH(OAM.StartDate)
                           AND YEAR(OCR.StartDate) = YEAR(OAM.StartDate)
                           AND OCR.EndDate >= CAST(GETUTCDATE() AS DATE)
                           AND CAST(OCR.StartDate AS DATE) >= @WindowStart
                           AND CAST(OCR.StartDate AS DATE) <= @WindowEnd
                   ),
                   CurrntMothNumberOfpassedOccrnes AS (
                     SELECT @SessionId SessionId,COUNT(OCR.OccurrenceId) OccerencesCount
                     FROM JustGoBookingScheduleOccurrence OCR
                       INNER JOIN JustGoBookingClassSessionSchedule SC ON SC.SessionScheduleId = OCR.ScheduleId
                       AND SC.IsDeleted = 0
                       AND OCR.IsDeleted = 0
                       LEFT JOIN JustGoBookingAdditionalHoliday AH ON AH.SessionId = SC.SessionId
                       AND AH.OccurrenceId = OCR.OccurrenceId
                     WHERE AH.SessionId IS NULL
                       AND SC.SessionId = @SessionId
                       AND OCR.StartDate < GETUTCDATE()
                       AND MONTH(OCR.StartDate) = MONTH(GETDATE())
                       AND YEAR(OCR.StartDate) = YEAR(GETDATE())
                       AND CAST(OCR.StartDate AS DATE) >= @WindowStart
                       AND CAST(OCR.StartDate AS DATE) <= @WindowEnd
                   ),
                   PriceOption AS (
                     SELECT POP.SessionId,POP.PriceOption,POP.Price,POP.IsDynamicPrice,POP.UnitPricePerSession,POP.ApplyProRataDiscount
                     FROM JustGoBookingClassSessionPriceOption POP
                     WHERE POP.SessionId = @SessionId
                       AND POP.IsEnable = 1
                   ),
                   OneOffOption AS (
                     SELECT POP.SessionId,POP.PriceOption,POP.Price,POP.ApplyProRataDiscount,POP.UnitPricePerSession
                     FROM PriceOption POP
                     WHERE POP.PriceOption = 1
                       AND ISNULL(POP.ApplyProRataDiscount, 0) = 0
                     UNION ALL
                     SELECT POP.SessionId,
                       POP.PriceOption,
                       
                       POP.Price  as Price,
                       POP.ApplyProRataDiscount,
                       POP.UnitPricePerSession
                     FROM PriceOption POP
                       INNER JOIN CurrntMothNumberOfpassedOccrnes NoOc ON NoOc.SessionId = POP.SessionId
                     WHERE POP.PriceOption = 1
                       and POP.ApplyProRataDiscount = 1
                   ),
                   MonthlyOption AS (
                     SELECT POP.SessionId,
                       POP.PriceOption,
                       POP.Price,
                       POP.IsDynamicPrice,
                       POP.ApplyProRataDiscount,
                       POP.UnitPricePerSession
                     FROM PriceOption POP
                     WHERE POP.PriceOption = 2
                       AND POP.IsDynamicPrice = 0
                     UNION ALL
                     SELECT POP.SessionId,
                       POP.PriceOption,
                       (POP.UnitPricePerSession * NoOc.OccerencesCount) Price,
                       POP.IsDynamicPrice,
                       POP.ApplyProRataDiscount,
                       POP.UnitPricePerSession
                     FROM PriceOption POP
                       INNER JOIN NumberOfOccrnes NoOc ON NoOc.SessionId = POP.SessionId
                     WHERE POP.PriceOption = 2
                       AND POP.IsDynamicPrice = 1
                   ),
                   PaygOption AS (
                     SELECT POP.SessionId,
                       POP.PriceOption,
                       POP.Price
                     FROM PriceOption POP
                     WHERE POP.PriceOption = 3
                   ),
                   TrialOption AS (
                     SELECT POP.SessionId,
                       POP.PriceOption,
                       POP.Price
                     FROM PriceOption POP
                     WHERE POP.PriceOption = 4
                   ),
                   WeeklyHours AS (
                     SELECT CSS.SessionId,
                       SUM(
                         CAST(
                           DATEDIFF(MINUTE, CSS.StartTime, CSS.EndTime) AS DECIMAL(10, 2)
                         ) / 60.0
                       ) AS TotalWeeklyHours
                     FROM JustGoBookingClassSessionSchedule CSS
                     WHERE CSS.SessionId = @SessionId
                       AND CSS.IsDeleted = 0
                     GROUP BY CSS.SessionId
                   ),
                    ProRataAdjustedPrices AS (
                      SELECT 
                        OneOffOption.SessionId,
                        OneOffOption.Price AS OriginalOneOffPrice,
                        MonthlyOption.Price AS OriginalMonthlyPrice
                      FROM OneOffOption
                      FULL OUTER JOIN MonthlyOption ON OneOffOption.SessionId = MonthlyOption.SessionId
                    )
                   SELECT CS.SessionId,
                     CS.[Name] SessionName,
                     CS.ClassSessionGuid SessionGuid,
                     CS.Capacity,
                     C.ClassId,
                     C.[Name] ClassName,
                     C.ClassGuid,
                     C.OwningEntitySyncGuid,
                     C.[Description],
                     case when c.StateId = 1 then 'Draft'
                   when c.StateId = 2 then 'Accepting Bookings'
                   when c.StateId = 3 then 'Closed for Bookings'
                   when c.StateId = 4 then 'Complete'
                   when c.StateId = 5 then 'Cancelled'
                   when c.StateId = 6 then 'Awaiting Approval'
                   when c.StateId = 7 then 'Template'
                   else 'Unknown' end as ClassState,
                     DATEADD(SECOND, X.gm_offset, CS.SessionBookingStartDate) BookingStartDate,
                     DATEADD(SECOND, X.gm_offset, CS.SessionBookingEndDate) BookingEndDate,
                     CC.CategoryId,
                     CC.[Name] CategoryName,
                     AG.Id AgeGroupId,
                     AG.[Name] AgeGroupName,
                     SOP.MinAge,
                     SOP.MaxAge,
                     SOP.Gender,
                     SOP.TrialLimit,
                     CG.ColorName,
                     CG.HexCode ColorCode,
                     IMGS.ClassImages,
                     IIF(OneOffOption.SessionId IS NULL, 0, 1) IsOneOffAvailable,
                     --OneOffOption.Price OneOffPrice,
                    -- ===== DISCOUNTED ONEOFF PRICE =====
                    CASE 
                      WHEN OneOffOption.ApplyProRataDiscount = 1 AND @OneOffProRataDiscount > 0 
                      THEN OneOffOption.Price - @OneOffProRataDiscount
                      ELSE OneOffOption.Price 
                    END AS OneOffPrice,
                     OneOffOption.ApplyProRataDiscount OneOffApplyProRataDiscount,
                     OneOffOption.UnitPricePerSession OneOffUnitPricePerSession,
                     IIF(MonthlyOption.SessionId IS NULL, 0, 1) IsMonthlyAvailable,
                     MonthlyOption.Price MonthlyPrice,
                     IIF(MonthlyOption.IsDynamicPrice = 1, 1, 0) IsDynamicAvailable,
                     MonthlyOption.ApplyProRataDiscount MonthlyApplyProRataDiscount,
                     MonthlyOption.UnitPricePerSession MonthlyUnitPricePerSession,
                     IIF(PaygOption.SessionId IS NULL, 0, 1) IsPaygAvailable,
                     PaygOption.Price PaygPrice,
                     IIF(TrialOption.SessionId IS NULL, 0, 1) IsTrialAvailable,
                     TrialOption.Price TrialPrice,
                     CSS.ScheduleInfo,
                     VD.Name VenueName,
                     VD.Address1 VenueAddress1,
                     VD.Address2 VenueAddress2,
                     VD.County VenueCounty,
                     VD.Postcode VenuePostcode,
                     VD.Region VenueRegion,
                     VD.Country VenueCountry,
                     VD.Latlng VenueLatlng,
                     IIF(CS.PricingMode = 2, 1, 0) AS IsHourlyPricingAvailable,
                     IIF(CS.PricingMode = 2, HPM.HourlyPrice, 0) AS HourlyPrice
                   FROM JustGoBookingClassSession CS
                     INNER JOIN JustGoBookingClass C ON C.ClassId = CS.ClassId
                     AND C.ClassId = @ClassId
                     AND CS.SessionId = @SessionId
                     INNER JOIN JustGoBookingClassSessionOption SOP ON SOP.SessionId = CS.SessionId
                     INNER JOIN JustGoBookingClassCategory CAT ON CAT.ClassId = C.ClassId
                     AND ISNULL(CAT.IsDeleted, 0) = 0
                     AND CAT.CategoryType = 1
                     INNER JOIN JustGoBookingCategory CC ON CC.CategoryId = CAT.CategoryId
                     AND CC.ParentId = -1
                     INNER JOIN SCHEDULE CSS ON CSS.SessionId = CS.SessionId
                     INNER JOIN Venue_Default VD ON VD.DocId = CS.VenueId
                     LEFT JOIN JustGoBookingAgeGroup AG ON AG.Id = CS.AgeGroupId
                     AND AG.IsActive = 1
                     LEFT JOIN JustGoBookingClassColorGroup CG ON CG.ColorGroupId = CS.ColorGroupId
                     LEFT JOIN IMGS ON IMGS.ClassId = C.ClassId
                     LEFT JOIN OneOffOption ON OneOffOption.SessionId = CS.SessionId
                     LEFT JOIN MonthlyOption ON MonthlyOption.SessionId = CS.SessionId
                     LEFT JOIN PaygOption ON PaygOption.SessionId = CS.SessionId
                     LEFT JOIN TrialOption ON TrialOption.SessionId = CS.SessionId
                     LEFT JOIN WeeklyHours WH ON WH.SessionId = CS.SessionId
                     OUTER APPLY (
                       SELECT TOP 1 PCD.MonthlyRate AS HourlyPrice
                       FROM JustGoBookingClassPricingChartDetail PCD
                       WHERE PCD.PricingChartId = CS.PricingChartId
                         AND PCD.IsDeleted = 0
                         AND PCD.HoursPerWeek <= WH.TotalWeeklyHours
                       ORDER BY PCD.HoursPerWeek DESC
                     ) HPM
                     OUTER APPLY (
                       SELECT TOP 1 *
                       FROM Timezone
                       WHERE zone_id = 161
                         AND time_start <= DATEDIFF(
                           SECOND,
                           '1970-01-01 00:00:00',
                           CS.SessionBookingStartDate
                         )
                       ORDER BY time_start DESC
                     ) AS X;
                   
                   SELECT C.[MemberId],
                     CONCAT(
                       ISNULL(C.FirstName, ''),
                       ' ',
                       ISNULL(C.LastName, '')
                     ) CoachName,
                     C.[Role],
                     IIF(
                       ISNULL(U.ProfilePicURL, '') = '',
                       '',
                       CONCAT(
                         @BaseUrl,
                         '/store/downloadpublic?f=',
                         U.ProfilePicURL,
                         '&t=user&p=',
                         U.UserId
                       )
                     ) AS ImageUrl
                   FROM JustGoBookingContact C
                     INNER JOIN [User] U ON U.MemberDocId = C.MemberDocId
                   WHERE [EntityId] = @SessionId;
                   
                   
                   SELECT OCR.OccurrenceId,
                     OCR.ScheduleId,
                     OCR.StartDate,
                     OCR.EndDate,
                     SIGN(ISNULL(AH.SessionId, 0)) IsHoliday,
                     IIF(OCR.EndDate >= GETUTCDATE(), 1, 0) IsFuture
                   FROM JustGoBookingScheduleOccurrence OCR
                     INNER JOIN JustGoBookingClassSessionSchedule SC ON SC.SessionScheduleId = OCR.ScheduleId
                     AND SC.IsDeleted != 1
                     AND OCR.IsDeleted != 1
                     AND SC.SessionId = @SessionId
                     LEFT JOIN JustGoBookingAdditionalHoliday AH ON AH.SessionId = @SessionId
                     AND AH.OccurrenceId = OCR.OccurrenceId
                   WHERE 
                       CAST(OCR.StartDate AS DATE) >= @WindowStart
                       AND CAST(OCR.StartDate AS DATE) <= @WindowEnd
                   ORDER BY OCR.StartDate ASC;
                   
                   
                   SELECT PCD.PricingChartDetailId AS Id,
                     PCD.HoursPerWeek,
                     PCD.MonthlyRate
                   FROM JustGoBookingClassSession CS
                     INNER JOIN JustGoBookingClassPricingChart PC ON CS.PricingChartId = PC.PricingChartId
                     INNER JOIN JustGoBookingClassPricingChartDetail PCD ON PC.PricingChartId = PCD.PricingChartId
                   WHERE CS.SessionId = @SessionId
                     AND PC.IsDeleted = 0
                     AND PCD.IsDeleted = 0
                   ORDER BY PCD.HoursPerWeek ASC;
                   """;

        await using var result = await _readRepository.GetLazyRepository<object>().Value.GetMultipleQueryAsync(sql, cancellationToken, queryParameters, null, "text");

        if (result is null)
        {
            return null;
        }

        var bookingClassDetails = await result.ReadSingleOrDefaultAsync<BookingClassDetails>();
        if (bookingClassDetails is null)
        {
            return null;
        }

        bookingClassDetails.SessionCoaches = (await result.ReadAsync<SessionCoach>()).AsList();
        bookingClassDetails.SessionOccurrences = (await result.ReadAsync<SessionOccurrence>()).AsList();
        bookingClassDetails.HourlyPricingCharts = (await result.ReadAsync<HourlyPricingChart>()).AsList();
        return bookingClassDetails;
    }

    private BookingClassDetailsDto MapToDto(BookingClassDetails details)
    {
        string[] genderArray = Array.Empty<string>();
        if (!string.IsNullOrWhiteSpace(details.Gender))
        {
            genderArray = details.Gender
                                      .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(s => s.Trim())
                                      .ToArray();
        }

        string[] classImagesArray = Array.Empty<string>();
        if (!string.IsNullOrWhiteSpace(details.ClassImages))
        {
            classImagesArray = details.ClassImages
                                           .Split('|', StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => s.Trim())
                                           .ToArray();
        }

        List<ScheduleInfoDto> scheduleInfoList = new List<ScheduleInfoDto>();
        if (!string.IsNullOrWhiteSpace(details.ScheduleInfo))
        {
            var scheduleEntries = details.ScheduleInfo.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var entry in scheduleEntries)
            {
                var parts = entry.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 3)
                {
                    scheduleInfoList.Add(new ScheduleInfoDto
                    {
                        Day = parts[0].Trim(),
                        StartTime = TimeOnly.Parse(parts[1].Trim()),
                        EndTime = TimeOnly.Parse(parts[2].Trim())
                    });
                }
            }
        }

        return new BookingClassDetailsDto
        {
            Class = new BookingSessionDto
            {
                SessionId = details.SessionId,
                SessionName = details.SessionName,
                SessionGuid = Guid.Parse(details.SessionGuid),
                Capacity = details.Capacity,
                ClassId = details.ClassId,
                ClassName = details.ClassName,
                ClassGuid = Guid.Parse(details.ClassGuid),
                OwningEntitySyncGuid = Guid.Parse(details.OwningEntitySyncGuid),
                Description = details.Description,
                ClassState = details.ClassState,
                BookingStartDate = details.BookingStartDate,
                BookingEndDate = details.BookingEndDate,
                CategoryId = details.CategoryId,
                CategoryName = details.CategoryName,
                AgeGroupId = details.AgeGroupId,
                AgeGroupName = details.AgeGroupName,
                MinAge = details.MinAge,
                MaxAge = details.MaxAge,
                Gender = genderArray,
                TrialLimit = details.TrialLimit,
                ColorName = details.ColorName,
                ColorCode = details.ColorCode,
                ClassImages = classImagesArray,
                IsOneOffAvailable = details.IsOneOffAvailable,
                OneOffPrice = details.OneOffPrice,
                //OneOffPrice = OneoffProRataPrice(details.OneOffApplyProRataDiscount, details.OneOffPrice, details.SessionOccurrences.Where(so=> so.IsHoliday == false).ToList().Count, details.SessionOccurrences.Where(so => so.IsHoliday == false).Count(o => o.IsFuture), details.OneOffUnitPricePerSession),
                IsMonthlyAvailable = details.IsMonthlyAvailable,
                IsDynamicAvailable = details.IsDynamicAvailable,
                IsHourlyPricingAvailable = details.IsHourlyPricingAvailable,
                MonthlyPrice = details.MonthlyPrice,
                HourlyPrice = details.HourlyPrice,
                //MonthlyPrice = MonthlyProRataPrice(details.MonthlyApplyProRataDiscount, details.MonthlyPrice, details.MonthlyUnitPricePerSession, details.SessionOccurrences.Count(o => o.IsFuture)),
                IsPaygAvailable = details.IsPaygAvailable,
                PaygPrice = details.PaygPrice,
                IsTrialAvailable = details.IsTrialAvailable,
                TrialPrice = details.TrialPrice
            },
            ScheduleInfo = scheduleInfoList,
            Venue = new SessionVenueDto
            {
                Name = details.VenueName,
                Address1 = details.VenueAddress1,
                Address2 = details.VenueAddress2,
                County = details.VenueCounty,
                Postcode = details.VenuePostcode,
                Region = details.VenueRegion,
                Country = details.VenueCountry,
                Latlng = details.VenueLatlng
            },
            Coaches = _mapper.Map<List<SessionCoachDto>>(details.SessionCoaches),
            Occurrences = _mapper.Map<List<SessionOccurrenceDto>>(details.SessionOccurrences),
            HourlyPricingChartDto = _mapper.Map<List<HourlyPricingChartDto>>(details.HourlyPricingCharts)
        };
    }

    private static bool GetWaitableStatus(bool waitlistOnly, int noOfInvite, bool allSessionsFull, int availableFullBookQty)
    {
        if (waitlistOnly && noOfInvite == 0) return true;
        else if (allSessionsFull && availableFullBookQty <= 0) return true;

        return false;
    }

    //private static decimal OneoffProRataPrice(bool oneOffApplyProRataDiscount, decimal oneoffPrice, int totalOccurrence, int upcomingOccurrence, decimal unitPricePerSession)
    //{
    //    if (!oneOffApplyProRataDiscount) return oneoffPrice;

    //    if (totalOccurrence <= 0) return oneoffPrice;

    //    int missedOccurenceCount = totalOccurrence - upcomingOccurrence;
    //    if (missedOccurenceCount <= 0) return oneoffPrice;

    //    decimal proRataPrice = oneoffPrice - (unitPricePerSession * missedOccurenceCount);
    //    proRataPrice = Math.Max(proRataPrice, 0);
    //    return Math.Round(proRataPrice, 2);
    //}

    //private static decimal MonthlyProRataPrice(bool monthlyApplyProRataDiscount, decimal monthlyPrice, decimal monthlyUnitPricePerSession, int upcomingOccurrenceCount)
    //{
    //    if (!monthlyApplyProRataDiscount) return monthlyPrice;
    //    decimal proRataPrice = monthlyUnitPricePerSession * upcomingOccurrenceCount;
    //    return Math.Round(proRataPrice, 2);
    //}

    private async Task<BookingSession?> GetBookingSessionAsync(GetClassDetailsQuery request, int userId, int memberId, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        queryParameters.Add("@ClassSessionGuid", request.SessionGuid.ToString() ,DbType.String, size: 100);
        queryParameters.Add("@UserId", userId);
        queryParameters.Add("@MemberId", memberId);

        string waitListInviteQuery = """
            SELECT S.SessionId, COUNT(1) NoOfInvite
            FROM JustGoBookingWaitList W
            INNER JOIN JustGoBookingWaitListHistory H ON H.WaitListId = W.WaitListId AND ISNULL(W.IsDeleted, 0) = 0
            INNER JOIN SESSSION S ON S.SessionId = H.SessionId
            INNER JOIN FAMILY FLM ON FLM.Entityid = W.EntityDocId AND FLM.Entityid > 0
            GROUP BY S.SessionId
            """;
        if (!string.IsNullOrWhiteSpace(request.WaitlistHistoryId) && userId == -1) //Through invite link and not logged in
        {
            waitListInviteQuery = """
                SELECT S.SessionId, COUNT(1) NoOfInvite
                FROM JustGoBookingWaitList W
                INNER JOIN JustGoBookingWaitListHistory H ON H.WaitListId = W.WaitListId AND ISNULL(W.IsDeleted, 0) = 0 AND H.HistoryGuid = @WaitlistHistoryId
                INNER JOIN SESSSION S ON S.SessionId = H.SessionId
                GROUP BY S.SessionId
                """;
            if (Guid.TryParse(request.WaitlistHistoryId, out var waitlistGuid))
            {
                queryParameters.Add("@WaitlistHistoryId", waitlistGuid);
            }
        }

        string reserveCondition = string.Empty;
        if (memberId > 0)
        {
            reserveCondition = $@"AND W.EntityDocId <> {memberId}";
        }
        else if (!string.IsNullOrWhiteSpace(request.WaitlistHistoryId))
        {
            reserveCondition = $@"AND H.HistoryGuid <> @WaitlistHistoryId";
        }

        var sql = $"""
        DECLARE @SessionId INT = (SELECT TOP 1 SessionId FROM JustGoBookingClassSession S WHERE S.ClassSessionGuid = @ClassSessionGuid);
        WITH
        SESSSION AS (
            SELECT S.SessionId, S.WaitlistOnly, S.Capacity
            FROM JustGoBookingClassSession S  
            WHERE S.ClassSessionGuid = @ClassSessionGuid
        ),
        OccrInfo AS (
            Select SessionId, OccurrenceId, AvailableQty, BookedQty
            from dbo.GetOccurenceBookingDetails(@SessionId, '')
        ),
        OCCR_BOOK AS (
            SELECT SDL.SessionId, OCR.OccurrenceId OccurenceId, OI.BookedQty
            FROM JustGoBookingClassSessionSchedule SDL
            INNER JOIN SESSSION S ON S.SessionId = SDL.SessionId
            INNER JOIN JustGoBookingScheduleOccurrence OCR ON OCR.ScheduleId = SDL.SessionScheduleId
            INNER JOIN OccrInfo OI ON OI.OccurrenceId = OCR.OccurrenceId
            WHERE OCR.EndDate >= CAST(GETUTCDATE() AS DATE)
        ),
        RESERVED AS (
            SELECT S.SessionId, COUNT(1) ReserveCount
            FROM JustGoBookingWaitListHistory H
            INNER JOIN JustGoBookingWaitlist W ON W.WaitListId = H.WaitListId 
            INNER JOIN SESSSION S ON S.SessionId = H.SessionId
            WHERE H.IsReserved = 1 AND H.ExpiredTime >= GETUTCDATE()
            {reserveCondition}
            -- AND -- H.HistoryGuid <> '4D7DA032-7BE6-40C4-82F7-E9155643B356' -- W.EntityDocId <> 87571--5286944
            GROUP BY S.SessionId
        ),
        SESSION_BOOK AS (
            SELECT SessionId, IIF(SUM(IIF(AvailableQty > 0, 1, 0)) = 0, 1, 0) AS AllSessionsFull, MIN(AvailableQty) AvailableFullBookQty
            FROM (
                SELECT CS.SessionId, GREATEST(((CS.Capacity - ISNULL(B.BookedQty, 0)) - ISNULL(R.ReserveCount, 0)), 0) AvailableQty
                FROM SESSSION CS 
                LEFT JOIN OCCR_BOOK B ON B.SessionId = CS.SessionId
                LEFT JOIN RESERVED R ON R.SessionId = CS.SessionId
            ) SB
            GROUP BY SessionId
        ),
        FAMILY AS (
            SELECT FLM.Entityid 
            FROM Family_Links FLM
            INNER JOIN (
                SELECT TOP 1 FL.DocId
                FROM Family_Links FL
                INNER JOIN MembersUsers_Default MD ON MD.DocId = FL.Entityid
                WHERE FL.Entityparentid = 1 AND MD.MemberUserId = @UserId
            ) TEMP ON TEMP.DocId = FLM.DocId AND FLM.Entityparentid = 1
            UNION
            SELECT @MemberId
        ),
        CTE_WAIT_LIST_INVITE AS (
            {waitListInviteQuery}
        )
        SELECT CS.SessionId, CS.WaitlistOnly, SB.AllSessionsFull, SB.AvailableFullBookQty, ISNULL(W.NoOfInvite, 0) NoOfInvite
        FROM SESSSION CS
        LEFT JOIN SESSION_BOOK SB ON SB.SessionId = CS.SessionId
        LEFT JOIN CTE_WAIT_LIST_INVITE W ON W.SessionId = CS.SessionId
        ;
        """;

        return await _readRepository.GetLazyRepository<BookingSession>().Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
    }
}
