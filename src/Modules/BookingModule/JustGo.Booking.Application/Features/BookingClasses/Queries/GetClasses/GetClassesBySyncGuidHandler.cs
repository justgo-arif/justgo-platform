using Dapper;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Booking.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System.Data;
using System.Text;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetWebletConfiguration;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetClasses;

public class GetClassesBySyncGuidHandler : IRequestHandler<GetClassesBySyncGuidQuery, KeysetPagedResult<BookingClassDto>>
{
    private readonly IReadRepositoryFactory _readRepository;
    private readonly IHybridCacheService _cache;
    private readonly IMediator _mediator;


    public GetClassesBySyncGuidHandler(IReadRepositoryFactory readRepository, IHybridCacheService cache, IMediator mediator)
    {
        _readRepository = readRepository;
        _cache = cache;
        _mediator = mediator;
    }

    public async Task<KeysetPagedResult<BookingClassDto>> Handle(GetClassesBySyncGuidQuery request, CancellationToken cancellationToken)
    {
        var webletSqlParts = (string.Empty, string.Empty);

        if (request.WebletGuid.HasValue && request.WebletGuid.Value != Guid.Empty)
        {
            var webletConfiguration = await _mediator.Send(new GetWebletConfigurationQuery((Guid)request.WebletGuid!), cancellationToken);

            if (webletConfiguration?.Config?.Filter is not null)
            {
                webletSqlParts = GetSqlFromWeblet(webletConfiguration, request);
            }
        }

        string cacheKey = $"justgobooking:classes:{request.OwningEntityGuid}:{request.LastSeenId}:{JsonConvert.SerializeObject(request)}";

        //await _cache.RemoveAsync(cacheKey, cancellationToken);
        //var data = await GetClassesAsync(request, cancellationToken);
        var cachedData = await _cache.GetOrSetAsync<List<BookingClass>>(
                                         cacheKey,
                                         async _ => await GetClassesAsync(request, webletSqlParts, cancellationToken),
                                         TimeSpan.FromMinutes(10),
                                         [nameof(CacheTag.Class)],
                                         cancellationToken
                                         );

        var data = new List<BookingClass>(cachedData); //to avoid mutating the cached object

        var hasMore = data.Count > request.NumberOfRow;
        if (hasMore)
            data.RemoveAt(data.Count - 1);

        List<BookingSession> bookingSessions = new List<BookingSession>();
        string sessionids = string.Join(",", data.Select(d => d.SessionId).Distinct());
        if (!string.IsNullOrEmpty(sessionids)) bookingSessions = await GetSessionInfoAsync(sessionids, cancellationToken);

        var classes = data.Select(bookingClass => MapToDto(bookingClass, bookingSessions, request)).ToList();

        return new KeysetPagedResult<BookingClassDto>()
        {
            Items = classes,
            TotalCount = request.TotalRows is > 0 ? request.TotalRows.Value : data.FirstOrDefault()?.TotalRows ?? 0,
            HasMore = hasMore,
            LastSeenId = data.LastOrDefault()?.RowNumber ?? 0
        };
    }

    private async Task<List<BookingClass>> GetClassesAsync(GetClassesBySyncGuidQuery request, (string joinWebletSql, string conditionWebletSql) webletSqlParts, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        queryParameters.Add("@OwningEntitySyncGuid", request.OwningEntityGuid.ToString(), DbType.String, size: 100);
        queryParameters.Add("@LastSeenId", request.LastSeenId ?? 0);
        queryParameters.Add("@NumberOfRows", request.NumberOfRow + 1);

        (string sortSql, string joinSql, string conditionSql, string cteWeekDayOrder) = AddQueryConditions(request, queryParameters);

        string webletJoin = "";
        string webletTopLevelCte = "";

        if (!string.IsNullOrWhiteSpace(webletSqlParts.joinWebletSql) || !string.IsNullOrWhiteSpace(webletSqlParts.conditionWebletSql))
        {
            webletTopLevelCte = $"""
                                    WEB_SESSION AS (
                                    SELECT 
                                    CS.SessionId
                                    FROM JustGoBookingClassSession CS
                                    INNER JOIN JustGoBookingClass C ON C.ClassId = CS.ClassId AND CS.IsDeleted = 0 AND C.IsDeleted = 0 AND C.ClassBookingType = 2
                                    {webletSqlParts.joinWebletSql}
                                    WHERE C.OwningEntitySyncGuid = @OwningEntitySyncGuid AND C.StateId = 2
                                    AND CS.SessionBookingEndDate >= CAST(GETUTCDATE() AS DATE) AND CAST(GETUTCDATE() AS DATE) >= CS.SessionBookingStartDate
                                    {webletSqlParts.conditionWebletSql}
                                ),
                                """;
            webletJoin = """INNER JOIN WEB_SESSION WS ON WS.SessionId = CS.SessionId""";
        }

        string totalRowsQuery = (request.TotalRows ?? 0) > 0 ? $"{request.TotalRows}" : "(SELECT COUNT(1) FROM DISTINCT_SESSION)";

        string daySelectionSql = "", sessionCTE = "SESSSION", groupBySql = "";
        if (request.SortBy.ToLower() == "day") //For multiple row if class has multiple days
        {
            daySelectionSql = ", [DayOfWeek]";
            sessionCTE = "(SELECT DISTINCT SessionId FROM SESSSION)";
            groupBySql = """
                GROUP BY 
                CS.SessionId, CS.[Name], CS.ClassSessionGuid, CS.Capacity, C.ClassId, C.[Name], 
                CC.CategoryId, CC.[Name], AG.Id, AG.[Name], C.ClassGuid, C.OwningEntitySyncGuid, 
                SOP.MinAge, SOP.MaxAge, SOP.Gender, CG.ColorName, CG.HexCode, IMGS.ClassImages,
                OneOffOption.Price, MonthlyOption.Price, PaygOption.Price, 
                CSS.ScheduleInfo,
                S.TotalRows, S.RowNumber,
                [DayOfWeek],CS.PricingMode,HPM.HourlyPrice
                """; //To exclude duplicates
        }

        //string prorataCondition = string.Empty;
        //if (request.CategoryGuid.HasValue)
        //{
        //    prorataCondition = " AND CC.CategoryGuid = @CategoryGuid";
        //}
        //if (request.AgeGroupId.HasValue)
        //{
        //    prorataCondition = " AND AG.Id = @AgeGroupId";
        //}
        string prorataCondition = BuildProRataCondition(request, queryParameters);

        var sql = $"""
                   DECLARE @BaseUrl Varchar(100) = (SELECT [Value] FROM SystemSettings Where ItemKey = 'SYSTEM.SITEADDRESS')
                   SET @BaseUrl = IIF(RIGHT(@BaseUrl, 1) = '/', LEFT(@BaseUrl, LEN(@BaseUrl) - 1), @BaseUrl);

                   DECLARE @ProRataResults TABLE (
                       SessionId INT,
                       ProductId INT,
                       ProductType INT,
                       PriceOption INT,
                       ProRataDiscount DECIMAL(10,2)
                   );
                   DECLARE @TempProRata TABLE (
                       ProRataDiscount DECIMAL(10,2)
                   );
                   DECLARE @ProductsForProRata TABLE (
                       RowNum INT IDENTITY(1,1),
                       SessionId INT,
                       ProductId INT,
                       ProductType INT,
                       PriceOption INT
                   );
                   
                   INSERT INTO @ProductsForProRata (SessionId, ProductId, ProductType, PriceOption)
                   SELECT DISTINCT
                       sp.SessionId,
                       sp.ProductId,
                       sp.ProductType,
                       spo.PriceOption
                   FROM JustGoBookingClassSession CS
                   INNER JOIN JustGoBookingClass C ON C.ClassId = CS.ClassId AND CS.IsDeleted = 0 AND C.IsDeleted = 0 AND C.ClassBookingType = 2
                   INNER JOIN JustGoBookingClassSessionSchedule SCHDL ON SCHDL.SessionId = CS.SessionId AND SCHDL.IsDeleted = 0
                   INNER JOIN JustGoBookingClassCategory CAT ON CAT.ClassId = C.ClassId AND CAT.IsDeleted = 0 AND CAT.CategoryType = 1
                   INNER JOIN JustGoBookingCategory CC ON CC.CategoryId = CAT.CategoryId AND CC.ParentId = -1
                   INNER JOIN JustGoBookingClassSessionProduct sp ON sp.SessionId = CS.SessionId AND ISNULL(sp.IsDeleted, 0) = 0
                   INNER JOIN JustGoBookingClassSessionPriceOption spo ON spo.SessionId = sp.SessionId AND spo.SessionPriceOptionId = sp.SessionPriceOptionId
                   LEFT JOIN JustGoBookingAgeGroup AG ON AG.Id = CS.AgeGroupId AND AG.IsActive = 1
                   WHERE C.OwningEntitySyncGuid = @OwningEntitySyncGuid
                       AND C.StateId = 2
                       AND CS.SessionBookingEndDate >= CAST(GETUTCDATE() AS DATE)
                       AND CAST(GETUTCDATE() AS DATE) >= CS.SessionBookingStartDate
                       { prorataCondition }
                       AND spo.IsEnable = 1
                       AND ISNULL(spo.ApplyProRataDiscount, 0) = 1
                       AND sp.ProductType IN (1, 4)
                       AND spo.PriceOption IN (1, 2);
                   
                   DECLARE @CurrentRow INT = 1;
                   DECLARE @TotalRows INT = (SELECT COUNT(*) FROM @ProductsForProRata);
                   DECLARE @SessionId INT, @ProductId INT, @ProductType INT, @PriceOption INT;
                   
                   WHILE @CurrentRow <= @TotalRows
                   BEGIN
                       SELECT 
                           @SessionId = SessionId,
                           @ProductId = ProductId,
                           @ProductType = ProductType,
                           @PriceOption = PriceOption
                       FROM @ProductsForProRata
                       WHERE RowNum = @CurrentRow;
                   
                       DELETE FROM @TempProRata;
                       
                       INSERT INTO @TempProRata (ProRataDiscount)
                       EXEC dbo.CalculateProRataDiscountByClassProduct 
                           @ClassProductDocId = @ProductId,
                           @EntityDocId = 0;
                       
                       INSERT INTO @ProRataResults (SessionId, ProductId, ProductType, PriceOption, ProRataDiscount)
                       SELECT @SessionId, @ProductId, @ProductType, @PriceOption, ProRataDiscount
                       FROM @TempProRata;
                   
                       SET @CurrentRow = @CurrentRow + 1;
                   END;
                   
                   
                   ;WITH 
                   {cteWeekDayOrder}
                   {webletTopLevelCte}
                   ALL_SESSION AS (
                       SELECT 
                       CS.SessionId, C.ClassId, ROW_NUMBER() OVER (ORDER BY {sortSql}) RowNumber {daySelectionSql}
                       FROM JustGoBookingClassSession CS
                       INNER JOIN JustGoBookingClass C ON C.ClassId = CS.ClassId AND CS.IsDeleted = 0 AND C.IsDeleted = 0 AND C.ClassBookingType = 2 
                       {webletJoin}
                       {joinSql}

                       WHERE C.OwningEntitySyncGuid = @OwningEntitySyncGuid AND C.StateId = 2
                       AND CS.SessionBookingEndDate >= CAST(GETUTCDATE() AS DATE) AND CAST(GETUTCDATE() AS DATE) >= CS.SessionBookingStartDate
                       {conditionSql}
                   ),
                   DISTINCT_SESSION AS (
                       SELECT 
                       S.SessionId, S.ClassId, MIN(S.RowNumber) RowNumber {daySelectionSql}
                       FROM ALL_SESSION S
                       GROUP BY S.SessionId, S.ClassId {daySelectionSql}
                   ),
                   SESSSION AS (
                       SELECT TOP (@NumberOfRows) S.SessionId, S.ClassId, S.RowNumber, {totalRowsQuery} TotalRows {daySelectionSql}
                       FROM DISTINCT_SESSION S  
                       WHERE RowNumber > @LastSeenId
                       ORDER BY RowNumber
                   ),
                   SCHEDULE AS (
                       SELECT SDL.SessionId, STRING_AGG((SDL.[DayOfWeek] + '|' + CAST(SDL.StartTime AS VARCHAR) + '|' + CAST(SDL.EndTime AS VARCHAR)), ',') ScheduleInfo,
                       SUM(CAST(DATEDIFF(MINUTE, SDL.StartTime, SDL.EndTime) AS DECIMAL(10,2)) / 60.0) AS TotalWeeklyHours
                       FROM 
                       (
                           SELECT DISTINCT CSS.SessionId, CSS.[DayOfWeek], CSS.StartTime, CSS.EndTime
                           FROM JustGoBookingClassSessionSchedule CSS
                           INNER JOIN SESSSION S ON S.SessionId = CSS.SessionId AND CSS.IsDeleted = 0
                       ) SDL
                       GROUP BY SDL.SessionId
                   ),
                   IMGS AS (
                       SELECT A.ClassId, STRING_AGG(CONCAT(@BaseUrl, '/store/downloadpublic?f=', A.[Name], '&t=justgobookingattachment&p=', A.EntityId, '&p1=', A.EntityTypeId), '|') ClassImages	
                       FROM (
                       SELECT S.ClassId, A.[Name], A.EntityId, A.EntityTypeId
                       FROM JustGoBookingAttachment A
                       INNER JOIN SESSSION S ON S.ClassId = A.EntityId AND A.EntityTypeId = 1 AND A.IsDeleted = 0
                       GROUP BY S.ClassId, A.[Name], A.EntityId, A.EntityTypeId
                       ) A
                       GROUP BY A.ClassId
                   ),
                   NumberOfOccrnes AS (
                       SELECT S.SessionId, COUNT(OCR.OccurrenceId) OccerencesCount
                       FROM JustGoBookingScheduleOccurrence OCR
                       INNER JOIN JustGoBookingClassSessionSchedule SC ON SC.SessionScheduleId = OCR.ScheduleId AND SC.IsDeleted = 0 AND OCR.IsDeleted = 0
                       INNER JOIN {sessionCTE} S ON S.SessionId = SC.SessionId
                       LEFT JOIN JustGoBookingAdditionalHoliday AH ON AH.SessionId = SC.SessionId AND AH.OccurrenceId = OCR.OccurrenceId
                       WHERE AH.SessionId IS NULL AND MONTH(OCR.StartDate) = MONTH(GETDATE()) AND YEAR(OCR.StartDate) = YEAR(GETDATE())
                       GROUP BY S.SessionId
                   ),
                   CurrntMothNumberOfpassedOccrnes AS (
                       SELECT S.SessionId, COUNT(OCR.OccurrenceId) OccerencesCount
                       FROM JustGoBookingScheduleOccurrence OCR
                       INNER JOIN JustGoBookingClassSessionSchedule SC ON SC.SessionScheduleId = OCR.ScheduleId AND SC.IsDeleted = 0 AND OCR.IsDeleted = 0
                       INNER JOIN {sessionCTE} S ON S.SessionId = SC.SessionId
                       LEFT JOIN JustGoBookingAdditionalHoliday AH ON AH.SessionId = SC.SessionId AND AH.OccurrenceId = OCR.OccurrenceId
                       WHERE AH.SessionId IS NULL 
                       --AND OCR.StartDate >= DATEFROMPARTS(YEAR(GETUTCDATE()), MONTH(GETUTCDATE()), 1)
                       AND OCR.StartDate < GETUTCDATE()
                       GROUP BY S.SessionId
                   ),
                   PriceOption AS (
                       SELECT POP.SessionId, POP.PriceOption, POP.Price, POP.IsDynamicPrice, POP.UnitPricePerSession,POP.ApplyProRataDiscount
                       FROM JustGoBookingClassSessionPriceOption POP
                       INNER JOIN {sessionCTE} S ON S.SessionId = POP.SessionId AND POP.IsEnable = 1
                   ),
                   ProRataDiscountsFromProcedure AS (
                       SELECT SessionId, ProductType, PriceOption, ProRataDiscount
                       FROM @ProRataResults
                   ),
                   
                   OneOffOption AS (
                       SELECT 
                           POP.SessionId, 
                           POP.PriceOption, 
                           CASE 
                               WHEN ISNULL(POP.ApplyProRataDiscount, 0) = 1 AND ISNULL(PRD.proRataDiscount, 0) > 0 
                               THEN POP.Price - PRD.proRataDiscount
                               ELSE POP.Price 
                           END AS Price
                       FROM PriceOption POP 
                       LEFT JOIN ProRataDiscountsFromProcedure PRD ON PRD.SessionId = POP.SessionId 
                           AND PRD.ProductType = 1 AND PRD.PriceOption = 1
                       WHERE POP.PriceOption = 1
                   ),
                   MonthlyOption AS (
                       SELECT POP.SessionId, POP.PriceOption, POP.Price
                       FROM PriceOption POP 
                       WHERE POP.PriceOption = 2 AND POP.IsDynamicPrice = 0
                           AND ISNULL(POP.ApplyProRataDiscount, 0) = 0
                       
                       UNION ALL
                       
                       SELECT 
                           POP.SessionId, POP.PriceOption, 
                           CASE 
                               WHEN ISNULL(PRD.proRataDiscount, 0) > 0 
                               THEN POP.Price - PRD.proRataDiscount
                               ELSE POP.Price 
                           END AS Price
                       FROM PriceOption POP 
                       LEFT JOIN ProRataDiscountsFromProcedure PRD ON PRD.SessionId = POP.SessionId 
                           AND PRD.ProductType = 4 AND PRD.PriceOption = 2
                       WHERE POP.PriceOption = 2 AND POP.IsDynamicPrice = 0
                           AND ISNULL(POP.ApplyProRataDiscount, 0) = 1
                       
                       UNION ALL
                       
                       SELECT POP.SessionId, POP.PriceOption, (POP.Price * NoOc.OccerencesCount) Price
                       FROM PriceOption POP 
                       INNER JOIN NumberOfOccrnes NoOc ON NoOc.SessionId = POP.SessionId
                       WHERE POP.PriceOption = 2 AND POP.IsDynamicPrice = 1
                   ),
                   PaygOption AS (
                       SELECT POP.SessionId, POP.PriceOption, POP.Price
                       FROM PriceOption POP WHERE POP.PriceOption = 3
                   )
                   SELECT CS.SessionId, CS.[Name] SessionName, CS.ClassSessionGuid SessionGuid, CS.Capacity, C.ClassId, C.[Name] ClassName, 
                   CC.CategoryId, CC.[Name] CategoryName, AG.Id AgeGroupId, AG.[Name] AgeGroupName,
                   C.ClassGuid, C.OwningEntitySyncGuid, SOP.MinAge, SOP.MaxAge, SOP.Gender, CG.ColorName, CG.HexCode ColorCode, IMGS.ClassImages,
                   OneOffOption.Price OneOffPrice,
                    MonthlyOption.Price MonthlyPrice, PaygOption.Price PaygPrice, HPM.HourlyPrice,
                   CSS.ScheduleInfo,
                   S.TotalRows, S.RowNumber
                   {daySelectionSql}
                   FROM SESSSION S
                   INNER JOIN JustGoBookingClassSession CS ON CS.SessionId = S.SessionId
                   INNER JOIN JustGoBookingClass C ON C.ClassId = CS.ClassId
                   INNER JOIN JustGoBookingClassSessionOption SOP ON SOP.SessionId = S.SessionId
                   INNER JOIN JustGoBookingClassCategory CAT ON CAT.ClassId = C.ClassId AND CAT.IsDeleted = 0 AND CAT.CategoryType = 1
                   INNER JOIN JustGoBookingCategory CC ON CC.CategoryId = CAT.CategoryId AND CC.ParentId = -1 
                   INNER JOIN SCHEDULE CSS ON CSS.SessionId = CS.SessionId
                   LEFT JOIN JustGoBookingAgeGroup AG ON AG.Id = CS.AgeGroupId AND AG.IsActive = 1
                   LEFT JOIN JustGoBookingClassColorGroup CG ON CG.ColorGroupId = CS.ColorGroupId
                   LEFT JOIN IMGS ON IMGS.ClassId = C.ClassId
                   LEFT JOIN OneOffOption ON OneOffOption.SessionId = CS.SessionId
                   LEFT JOIN MonthlyOption ON MonthlyOption.SessionId = CS.SessionId
                   LEFT JOIN PaygOption ON PaygOption.SessionId = CS.SessionId
                   OUTER APPLY
                   (
                       SELECT TOP 1 PCD.MonthlyRate HourlyPrice
                       FROM JustGoBookingClassPricingChartDetail PCD
                       WHERE PCD.PricingChartId = CS.PricingChartId AND PCD.IsDeleted = 0 AND PCD.HoursPerWeek <= CSS.TotalWeeklyHours AND CS.PricingMode = 2
                       ORDER BY PCD.HoursPerWeek DESC
                   ) HPM
                   {groupBySql}
                   ORDER BY S.RowNumber ASC;
                   """;

        return (await _readRepository.GetLazyRepository<BookingClass>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
    }


    private (string, string, string, string) AddQueryConditions(GetClassesBySyncGuidQuery request, DynamicParameters parameters)
    {
        bool isWorkDayOrderExist = false, isScheduleExist = false, isCategoryExist = false, isAgeGroupExist = false, isSOption = false, isColorGroup = false,
            isPriceOption = false;

        string joinSql = "", conditionSql = "", sortSql = "", cteWeekDayOrder = "";

        #region SORT & ORDER
        string orderBy = request.OrderBy.ToUpper() == "DESC" ? "DESC" : "ASC";
        isScheduleExist = true;
        isWorkDayOrderExist = true;
        if (request.SortBy.ToLower() == "day")
        {
            sortSql = $"W.[SortOrder] {orderBy}, SCHDL.StartTime {orderBy}";
        }
        else if (request.SortBy.ToLower() == "class group")
        {
            sortSql = $"C.[Name] {orderBy}, W.[SortOrder] {orderBy}";
        }
        else if (request.SortBy.ToLower() == "colour")
        {
            sortSql = $"ISNULL(COL.ColorName, 'ZZZ') {orderBy}, W.[SortOrder] {orderBy}";
            isColorGroup = true;
        }
        else if (request.SortBy.ToLower() == "discipline")
        {
            sortSql = $"CC.[Name] {orderBy}, W.[SortOrder] {orderBy}";
            isCategoryExist = true;
        }
        else if (request.SortBy.ToLower() == "age group")
        {
            sortSql = $"ISNULL(AG.MinAge, 200) {orderBy}, ISNULL(AG.MaxAge, 200) {orderBy}, AG.[Name] {orderBy}, W.[SortOrder] {orderBy}";
            isAgeGroupExist = true;
        }
        #endregion

        #region FILTERS
        //EITHER CategoryGuid OR AgeGroupId MUST BE PROVIDED
        if (request.CategoryGuid.HasValue)
        {
            isCategoryExist = true;
            parameters.Add("@CategoryGuid", request.CategoryGuid.Value);
            conditionSql += " AND CC.CategoryGuid = @CategoryGuid";
        }

        if (request.AgeGroupId.HasValue)
        {
            isAgeGroupExist = true;
            parameters.Add("@AgeGroupId", request.AgeGroupId.Value);
            conditionSql += " AND AG.Id = @AgeGroupId";
        }
        //EITHER CategoryGuid OR AgeGroupId MUST BE PROVIDED

        if (request.Days?.Length > 0)
        {
            isScheduleExist = true;
            var paramNames = request.Days.Select((d, i) => $"@Day{i}").ToArray();
            var sqlInClause = string.Join(",", paramNames);
            for (int i = 0; i < request.Days.Length; i++)
                parameters.Add(paramNames[i], request.Days[i]);

            conditionSql += $" AND SCHDL.[DayOfWeek] IN ({sqlInClause})";
        }

        if (request.Disciplines?.Length > 0)
        {
            isCategoryExist = true;
            var paramNames = request.Disciplines.Select((d, i) => $"@Discipline{i}").ToArray();
            var sqlInClause = string.Join(",", paramNames);
            for (int i = 0; i < request.Disciplines.Length; i++)
                parameters.Add(paramNames[i], request.Disciplines[i]);

            conditionSql += $" AND CC.CategoryId IN ({sqlInClause})";
        }

        if (request.AgeGroups?.Length > 0)
        {
            var paramNames = request.AgeGroups.Select((d, i) => $"@AgeGroup{i}").ToArray();
            var sqlInClause = string.Join(",", paramNames);
            for (int i = 0; i < request.AgeGroups.Length; i++)
                parameters.Add(paramNames[i], request.AgeGroups[i]);

            conditionSql += $" AND CS.AgeGroupId IN ({sqlInClause})";
        }

        if (request.ClassGroups?.Length > 0)
        {
            var paramNames = request.ClassGroups.Select((d, i) => $"@ClassGroup{i}").ToArray();
            var sqlInClause = string.Join(",", paramNames);
            for (int i = 0; i < request.ClassGroups.Length; i++)
                parameters.Add(paramNames[i], request.ClassGroups[i]);

            conditionSql += $" AND C.ClassId IN ({sqlInClause})";
        }

        if (request.Genders?.Length > 0)
        {
            isSOption = true;
            parameters.Add("@Gender", string.Join("|", request.Genders));

            conditionSql += @"
                AND 
                EXISTS (
                    SELECT 1
                    FROM STRING_SPLIT(SOP.Gender, ',')
                    WHERE value IN (SELECT Value FROM STRING_SPLIT(@Gender,'|'))
                )
                ";
        }

        if (request.ColorGroups?.Length > 0)
        {
            var paramNames = request.ColorGroups.Select((d, i) => $"@ColorGroup{i}").ToArray();
            var sqlInClause = string.Join(",", paramNames);
            for (int i = 0; i < request.ColorGroups.Length; i++)
                parameters.Add(paramNames[i], request.ColorGroups[i]);

            conditionSql += $" AND CS.ColorGroupId IN ({sqlInClause})";
        }

        if (request.Times?.Length > 0)
        {
            isScheduleExist = true;
            StringBuilder queryBuilder = new StringBuilder();
            int cont = 0;
            for (int i = 0; i < request.Times.Length; i++)
            {
                var times = request.Times[i].Split('-');
                if (times.Length != 2) continue;

                parameters.Add($"@StartTime{i}", times[0]);
                parameters.Add($"@EndTime{i}", times[1]);

                if (cont != 0) queryBuilder.Append(" OR ");
                queryBuilder.Append($"(SCHDL.StartTime BETWEEN @StartTime{i} AND @EndTime{i})");
                cont++;
            }
            conditionSql += $@"
                AND (
                    {queryBuilder.ToString()}
                )
                ";
        }

        if (request.Durations?.Length > 0)
        {
            isScheduleExist = true;
            bool hasOver4 = request.Durations.Any(d => d >= 240);
            request.Durations = request.Durations.Where(d => d < 240).ToArray();

            StringBuilder durationBuilder = new();
            if (request.Durations.Any())
            {
                var inClause = string.Join(",", request.Durations);
                durationBuilder.Append($"DATEDIFF(MINUTE, SCHDL.StartTime, SCHDL.EndTime) IN ({inClause})");
            }
            if (hasOver4)
            {
                if (durationBuilder.Length > 0)
                    durationBuilder.Append(" OR ");
                durationBuilder.Append("DATEDIFF(MINUTE, SCHDL.StartTime, SCHDL.EndTime) >= 240");
            }

            conditionSql += $@"
                AND (
                    {durationBuilder.ToString()}
                )
            ";
        }

        if (request.Payments?.Length > 0)
        {
            isPriceOption = true;
            var paramNames = request.Payments.Select((d, i) => $"@Payment{i}").ToArray();
            var sqlInClause = string.Join(",", paramNames);
            for (int i = 0; i < request.Payments.Length; i++)
                parameters.Add(paramNames[i], request.Payments[i]);

            conditionSql += $" AND POP.PriceOption IN ({sqlInClause})";
        }
        #endregion

        #region JOINS
        if (isScheduleExist)
        {
            joinSql += @"
                INNER JOIN JustGoBookingClassSessionSchedule SCHDL ON SCHDL.SessionId = CS.SessionId AND SCHDL.IsDeleted = 0
                ";
        }
        if (isWorkDayOrderExist)
        {
            cteWeekDayOrder = """
                WeekDayOrder AS (
                    SELECT 'Mon' [DayName], 1 [SortOrder] UNION ALL
                    SELECT 'Tue', 2 UNION ALL
                    SELECT 'Wed', 3 UNION ALL
                    SELECT 'Thu', 4 UNION ALL
                    SELECT 'Fri', 5 UNION ALL
                    SELECT 'Sat', 6 UNION ALL
                    SELECT 'Sun', 7
                ),
                """;
            joinSql += @"
                INNER JOIN WeekdayOrder W ON W.[DayName] = SCHDL.[DayOfWeek]
                ";
        }
        if (isCategoryExist)
        {
            joinSql += @"
                INNER JOIN JustGoBookingClassCategory CAT ON CAT.ClassId = C.ClassId AND CAT.IsDeleted = 0 AND CAT.CategoryType = 1
                INNER JOIN JustGoBookingCategory CC ON CC.CategoryId = CAT.CategoryId AND CC.ParentId = -1 
                ";
        }
        if (isAgeGroupExist)
        {
            joinSql += @"
            LEFT JOIN JustGoBookingAgeGroup AG ON AG.Id = CS.AgeGroupId AND AG.IsActive = 1
            ";
        }
        if (isSOption)
        {
            joinSql += @"
            INNER JOIN JustGoBookingClassSessionOption SOP ON SOP.SessionId = CS.SessionId AND SOP.IsDeleted = 0
            ";
        }
        if (isColorGroup)
        {
            joinSql += @"
            LEFT JOIN JustGoBookingClassColorGroup COL ON COL.ColorGroupId = CS.ColorGroupId
            ";
        }
        if (isPriceOption)
        {
            joinSql += @"
            INNER JOIN JustGoBookingClassSessionPriceOption POP ON POP.SessionId = CS.SessionId AND POP.IsEnable = 1
            ";
        }

        #endregion

        return (sortSql, joinSql, conditionSql, cteWeekDayOrder);

    }

    private static BookingClassDto MapToDto(BookingClass bookingClass, List<BookingSession> bookingSessions, GetClassesBySyncGuidQuery request)
    {
        string[] genderArray = Array.Empty<string>();
        if (!string.IsNullOrWhiteSpace(bookingClass.Gender))
        {
            genderArray = bookingClass.Gender
                                      .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(s => s.Trim())
                                      .ToArray();
        }

        string[] classImagesArray = Array.Empty<string>();
        if (!string.IsNullOrWhiteSpace(bookingClass.ClassImages))
        {
            classImagesArray = bookingClass.ClassImages
                                           .Split('|', StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => s.Trim())
                                           .ToArray();
        }

        List<ScheduleInfoDto> scheduleInfoList = new List<ScheduleInfoDto>();
        if (!string.IsNullOrWhiteSpace(bookingClass.ScheduleInfo))
        {
            var scheduleEntries = bookingClass.ScheduleInfo.Split(',', StringSplitOptions.RemoveEmptyEntries);
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

        BookingSession bookingSession = bookingSessions.FirstOrDefault(bs => bs.SessionId == bookingClass.SessionId)
        ?? new BookingSession
        {
            SessionId = bookingClass.SessionId,
            AllSessionsFull = false,
            AvailableFullBookQty = bookingClass.Capacity,
            WaitlistOnly = false
        };

        return new BookingClassDto
        {
            SessionName = bookingClass.SessionName,
            SessionGuid = bookingClass.SessionGuid,
            Capacity = bookingClass.Capacity,
            ClassName = bookingClass.ClassName,
            ClassGuid = bookingClass.ClassGuid,
            CategoryName = bookingClass.CategoryName,
            AgeGroupName = bookingClass.AgeGroupName,
            OwningEntitySyncGuid = bookingClass.OwningEntitySyncGuid,
            MinAge = bookingClass.MinAge,
            MaxAge = bookingClass.MaxAge,
            Gender = genderArray,
            ColorName = bookingClass.ColorName,
            ColorCode = bookingClass.ColorCode,
            ClassImages = classImagesArray,
            OneOffPrice = bookingClass.OneOffPrice,
            MonthlyPrice = bookingClass.MonthlyPrice,
            PaygPrice = bookingClass.PaygPrice,
            HourlyPrice = bookingClass.HourlyPrice,
            ScheduleInfo = scheduleInfoList.OrderBy(s => s.Day is not null ? DayOrderMap.GetValueOrDefault(s.Day, 8) : 8).ToList(),
            GroupBy = GetGroupByValue(bookingClass, request),
            AvailabilityStatus = GetAvailabilityStatus(bookingSession.AllSessionsFull, bookingSession.AvailableFullBookQty, bookingClass.Capacity),
            IsWaitable = GetWaitableStatus(bookingSession.WaitlistOnly, bookingSession.AllSessionsFull, bookingSession.AvailableFullBookQty),
        };
    }

    private static readonly IReadOnlyDictionary<string, int> DayOrderMap = new Dictionary<string, int>
    {
        ["Mon"] = 1,
        ["Tue"] = 2,
        ["Wed"] = 3,
        ["Thu"] = 4,
        ["Fri"] = 5,
        ["Sat"] = 6,
        ["Sun"] = 7
    };
    private static string BuildProRataCondition(GetClassesBySyncGuidQuery request, DynamicParameters parameters)
    {
        var predicates = new List<string>(capacity: 2);

        if (request.CategoryGuid.HasValue)
        {
            AddParameterIfMissing(parameters, "@CategoryGuid", request.CategoryGuid.Value, DbType.Guid);
            predicates.Add("CC.CategoryGuid = @CategoryGuid");
        }

        if (request.AgeGroupId.HasValue)
        {
            AddParameterIfMissing(parameters, "@AgeGroupId", request.AgeGroupId.Value, DbType.Int32);
            predicates.Add("AG.Id = @AgeGroupId");
        }

        return predicates.Count == 0
            ? string.Empty
            : " AND " + string.Join(" AND ", predicates);
    }

    private static void AddParameterIfMissing(DynamicParameters parameters, string name, object value, DbType dbType)
    {
        if (!parameters.ParameterNames.Any(p => string.Equals(p, name, StringComparison.OrdinalIgnoreCase)))
        {
            parameters.Add(name, value, dbType);
        }
    }
    private static string GetAvailabilityStatus(bool allSessionsFull, int availableFullBookQty, int capacity)
    {
        if (allSessionsFull && availableFullBookQty == 0)
        {
            return "Class Full";
        }

        if (!allSessionsFull && availableFullBookQty < capacity * 0.25)
        {
            return "Limited Places";
        }

        return availableFullBookQty > 1 ? $"{availableFullBookQty} sessions remaining" : $"{availableFullBookQty} session remaining";
    }

    private static string? GetGroupByValue(BookingClass bookingClass, GetClassesBySyncGuidQuery request)
    {
        return request.SortBy.ToLowerInvariant() switch
        {
            "day" => bookingClass.DayOfWeek,
            "class group" => bookingClass.ClassName,
            "colour" => bookingClass.ColorName,
            "discipline" => bookingClass.CategoryName,
            "age group" => bookingClass.AgeGroupName,
            _ => bookingClass.DayOfWeek
        };
    }

    private static bool GetWaitableStatus(bool waitlistOnly, bool allSessionsFull, int availableFullBookQty)
    {
        if (waitlistOnly) return true;
        else if (allSessionsFull && availableFullBookQty <= 0) return true;

        return false;
    }

    private async Task<List<BookingSession>> GetSessionInfoAsync(string sessionids, CancellationToken cancellationToken)
    {
        var sql = $"""
            WITH SESSSION AS (
            	SELECT S.SessionId, S.WaitlistOnly
                FROM JustGoBookingClassSession S  
                WHERE S.SessionId IN ({sessionids})
            ),
            ATTENDEE_INFO AS (
                SELECT A.SessionId, A.EntityDocId, A.[Status], AD.OccurenceId, AD.AttendeeType , AD.AttendeeDetailsStatus
                FROM JustGoBookingAttendee A 
                INNER JOIN SESSSION S ON S.SessionId = A.SessionId
                INNER JOIN JustGoBookingAttendeeDetails AD ON AD.AttendeeId = A.AttendeeId AND AD.AttendeeDetailsStatus = 1                
            ),
            OCCR_BOOK AS (
            	SELECT S.SessionId, OCR.OccurrenceId OccurenceId, COUNT(A.SessionId) AS BookedQty
                FROM JustGoBookingClassSessionSchedule SDL
                INNER JOIN SESSSION S ON S.SessionId = SDL.SessionId
                INNER JOIN JustGoBookingScheduleOccurrence OCR ON OCR.ScheduleId =SDL.SessionScheduleId
                LEFT JOIN ATTENDEE_INFO A ON A.OccurenceId = OCR.OccurrenceId AND A.SessionId = SDL.SessionId
                WHERE OCR.EndDate >= CAST(GETUTCDATE() AS DATE)
                GROUP BY S.SessionId, OCR.OccurrenceId
            ),
            SESSION_BOOK AS (
            	SELECT SessionId, IIF(SUM(IIF(AvailableQty > 0, 1, 0)) = 0, 1, 0) AS AllSessionsFull, MIN(AvailableQty) AvailableFullBookQty
            	FROM (
               		SELECT S.SessionId, (CS.Capacity - ISNULL(B.BookedQty, 0)) AvailableQty
               		FROM SESSSION S
               		INNER JOIN JustGoBookingClassSession CS ON CS.SessionId = S.SessionId
               		LEFT JOIN OCCR_BOOK B ON B.SessionId = S.SessionId
            	) SB
            	GROUP BY SessionId
            )
            SELECT S.SessionId, S.WaitlistOnly,
            SB.AllSessionsFull, SB.AvailableFullBookQty
            FROM SESSSION S
            LEFT JOIN SESSION_BOOK SB ON SB.SessionId = S.SessionId
            ;
            """;

        return (await _readRepository.GetLazyRepository<BookingSession>().Value.GetListAsync(sql, cancellationToken, null, null, "text")).ToList();
    }

    private static (string joinWebletSql, string conditionWebletSql) GetSqlFromWeblet(
       WebletConfigurationResponse webletConfiguration,
       GetClassesBySyncGuidQuery request)
    {
        var filter = webletConfiguration.Config.Filter;

        var conditions = new List<string>();
        var joins = new List<string>();

        bool requiresScheduleJoin = false;
        bool requiresCategoryJoin = false;
        bool requiresPriceOptionJoin = false;

        // AgeGroup filter
        if (filter.AgeGroups is { Count: > 0 })
        {
            var ageGroupConditions = string.Join(", ", filter.AgeGroups);
            conditions.Add($"CS.AgeGroupId IN ({ageGroupConditions})");
        }

        // Category filter
        if (filter.Categories is { Count: > 0 })
        {
            requiresCategoryJoin = true;
            var categoryConditions = string.Join(", ", filter.Categories);
            conditions.Add($"CC.CategoryId IN ({categoryConditions})");
        }

        // ClassGroups filter
        if (filter.ClassGroups is { Count: > 0 })
        {
            var classGroupConditions = string.Join(", ", filter.ClassGroups);
            conditions.Add($"C.ClassId IN ({classGroupConditions})");
        }

        // ColorGroups filter
        if (filter.ColorGroups is { Count: > 0 })
        {
            var colorGroupConditions = string.Join(", ", filter.ColorGroups);
            conditions.Add($"CS.ColorGroupId IN ({colorGroupConditions})");
        }

        // PaymentTypes filter
        if (filter.PaymentTypes is { Count: > 0 })
        {
            requiresPriceOptionJoin = true;
            var paymentTypeConditions = string.Join(", ", filter.PaymentTypes);
            conditions.Add($"POP.PriceOption IN ({paymentTypeConditions})");
        }

        // TimeOfDay filter
        if (filter.TimeOfDay is { Count: > 0 })
        {
            requiresScheduleJoin = true;

            var timeConditions = string.Join(" OR ", filter.TimeOfDay.Select(t =>
            {
                var times = t.Split('-');
                return times.Length == 2
                    ? $"(SCHDL.StartTime BETWEEN '{times[0]}' AND '{times[1]}')"
                    : string.Empty;
            }).Where(cond => !string.IsNullOrEmpty(cond)));

            if (!string.IsNullOrEmpty(timeConditions))
            {
                conditions.Add($"({timeConditions})");
            }
        }

        // ClassDuration filter
        if (filter.ClassDuration is { Count: > 0 })
        {
            requiresScheduleJoin = true;

            var durationConditions = string.Join(" OR ", filter.ClassDuration.Select(d =>
                d >= 240
                    ? "DATEDIFF(MINUTE, SCHDL.StartTime, SCHDL.EndTime) >= 240"
                    : $"DATEDIFF(MINUTE, SCHDL.StartTime, SCHDL.EndTime) = {d}"));

            conditions.Add($"({durationConditions})");
        }

        if (requiresScheduleJoin)
        {
            joins.Add(@"
            INNER JOIN JustGoBookingClassSessionSchedule SCHDL
                ON SCHDL.SessionId = CS.SessionId AND SCHDL.IsDeleted = 0
            ");
        }

        if (requiresCategoryJoin)
        {
            joins.Add(@"
            INNER JOIN JustGoBookingClassCategory CAT
                ON CAT.ClassId = C.ClassId AND CAT.IsDeleted = 0 AND CAT.CategoryType = 1
            INNER JOIN JustGoBookingCategory CC ON CC.CategoryId = CAT.CategoryId AND CC.ParentId = -1 
            ");
        }

        if (requiresPriceOptionJoin)
        {
            joins.Add(@"
            INNER JOIN JustGoBookingClassSessionPriceOption POP
                ON POP.SessionId = CS.SessionId AND POP.IsEnable = 1
            ");
        }

        string joinWebletSql = joins.Count > 0 ? string.Join("", joins) : string.Empty;
        string conditionWebletSql = conditions.Count > 0 ? "\nAND " + string.Join(" AND ", conditions) : string.Empty;

        return (joinWebletSql, conditionWebletSql);
    }

}
