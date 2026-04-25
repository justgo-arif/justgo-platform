using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Domain.Entities;
using MobileApps.Domain.Entities.V2.FieldManagement;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.FieldManagement.Queries.GetFMSystemFormCollection
{
    class MemberTicketDetailsQueryHandler : IRequestHandler<MemberTicketDetailsQuery, IList<IDictionary<string,object>>>
    {
        private readonly LazyService<IReadRepository<dynamic>> _readRepository;
        private IMediator _mediator;
        public MemberTicketDetailsQueryHandler(LazyService<IReadRepository<dynamic>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<IList<IDictionary<string, object>>> Handle(MemberTicketDetailsQuery request, CancellationToken cancellationToken)
        {
            string query = $@"
            DECLARE
            @UserId INT = {request.UserId},
            @MemberId INT = {request.MemberId},
            @SessionId VARCHAR(100) ={request.SessionId},
            @UtcDate Date = CAST(GETUTCDATE() AS DATE);";

            query += GetTicketQueryBySessionGuid();

            //var param = new DynamicParameters();
            //param.Add("@UserId", request.UserId);
            //param.Add("@MemberId", request.MemberId);
            //param.Add("@SessionId", request.SessionId);

            var result = await _readRepository.Value.GetListAsync(query, null, null, "text");
            return JsonConvert.DeserializeObject<IList<IDictionary<string,object>>>(JsonConvert.SerializeObject(result));
        }

        private string GetTicketQueryBySessionGuid()
        {
            return $@"WITH CLASS_SESSIONS AS (
                SELECT C.ClassId, S.SessionId, C.OwningEntityId, S.[Name] SessionName, S.Capacity, S.VenueId, S.TermId, S.SessionType, S.SessionReference, S.[Description] SessionDescription, S.TimeZoneId
                FROM JustGoBookingClass C 
                INNER JOIN JustGoBookingClassSession S ON S.ClassId = C.ClassId
	             WHERE s.SessionId = @SessionId
	            AND C.StateId = 2 AND ISNULL(C.IsDeleted, 0) = 0 AND ISNULL(S.IsDeleted, 0) = 0
            ),
            CLASS_PRODS AS (
	            SELECT S.SessionId, P.ProductId, P.ProductType 
	            FROM CLASS_SESSIONS S
	            INNER JOIN JustGoBookingClassSessionProduct P ON P.SessionId = S.SessionId
	            EXCEPT
				SELECT S.SessionId, P.ProductId, P.ProductType  
				FROM CLASS_SESSIONS S
				INNER JOIN JustGoBookingClassSessionProduct P ON P.SessionId = S.SessionId AND P.ProductType = 2
				INNER JOIN JustGoBookingClassSessionOption OP ON OP.SessionId = S.SessionId AND OP.IsTrial=0
            ),
            SESSION_PAYMENTS AS (
	            SELECT S.SessionId, PR.CourseBookingId
	            FROM CLASS_SESSIONS S
	            INNER JOIN JustGoBookingPaymentReference PR ON PR.SessionId = S.SessionId
            ),
            SESSION_OPTIONS AS (
	            SELECT O.SessionId, MAX(1) PriceOption, MAX(O.MembershipIds) MembershipIds
	            FROM JustGoBookingClassSessionOption O 
	            INNER JOIN CLASS_SESSIONS S ON S.SessionId = O.SessionId AND ISNULL(O.IsDeleted, 0) = 0
				GROUP BY O.SessionId
            ),
            SESSION_SCHEDULES AS (
                SELECT S.SessionId, STRING_AGG(SC.[DayOfWeek], '|') ScheduleDays, STRING_AGG(SC.StartTime, '|') ScheduleStartTimes, STRING_AGG(SC.EndTime, '|') ScheduleEndTimes
	            FROM JustGoBookingClassSessionSchedule SC
	            INNER JOIN CLASS_SESSIONS S ON S.SessionId = SC.SessionId AND ISNULL(SC.IsDeleted, 0) = 0
	            GROUP BY S.SessionId
            ),
            OCCURENCE AS (
	            SELECT S.SessionId, COUNT(S.SessionId) NoOfSessions
	            FROM JustGoBookingClassSessionSchedule SC
	            INNER JOIN CLASS_SESSIONS S ON S.SessionId = SC.SessionId AND ISNULL(SC.IsDeleted, 0) = 0
	            INNER JOIN JustGoBookingScheduleOccurrence OCR ON OCR.ScheduleId = SC.SessionScheduleId AND ISNULL(OCR.IsDeleted, 0) = 0 --AND OCR.EntityTypeId = unknown
	            GROUP BY S.SessionId
            ),
            CD AS (
	            SELECT CD.DocId, cd.ClubName, cd.ClubType, cd.[Location]
	            FROM Clubs_Default CD
	            INNER JOIN CLASS_SESSIONS CS ON CS.OwningEntityid = CD.DocId
	            --INNER JOIN PD ON CD.DocId = PD.OwningEntityid
	            GROUP BY CD.DocId, cd.ClubName, cd.ClubType, cd.[Location]
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
            BOOKED_PRODS AS (
	            SELECT CB.DocId, CB.CourseDocId, CB.Productdocid, CB.Entityid, CB.Bookingentityid, CB.Quantity,cb.Groupid
	            FROM SESSION_PAYMENTS SP
	            INNER JOIN CourseBooking_Default CB ON CB.DocId = SP.CourseBookingId
				LEFT JOIN FAMILY FLM ON FLM.Entityid = CB.Entityid 
	            WHERE 
				FLM.Entityid IS NOT NULL OR CB.Bookingentityid = @MemberId
            ),
            BOOK AS (
	            SELECT 
                    CB.CourseDocId SessionId, COUNT(1) NoOfBooking
                FROM BOOKED_PRODS CB
                GROUP BY CB.CourseDocId
            ),
            CTE_BOOK_STAT AS (
                SELECT S.SessionId, COUNT(1) TotalBookedCount, COUNT(CASE WHEN SP.ProductType = 1 THEN 1 END) BookedCount, COUNT(CASE WHEN SP.ProductType = 2 THEN 1 END) TrialCount
                FROM CLASS_SESSIONS S
                INNER JOIN JustGoBookingClassSessionProduct SP ON SP.SessionId = S.SessionId AND ISNULL(SP.IsDeleted, 0) = 0
                INNER JOIN CourseBooking_Default CBD ON CBD.Productdocid = SP.ProductId
	            INNER JOIN JustGoBookingPaymentReference PAY ON PAY.SessionId = SP.SessionId AND PAY.CourseBookingId = CBD.DocId AND PAY.TrialState NOT IN (2,3) --Expired, Canceled
                GROUP BY S.SessionId
            )
            SELECT S.ClassId, S.SessionId, CP.ProductType TicketType, O.PriceOption TicketPriceOption, O.MembershipIds, S.SessionName, S.Capacity, S.VenueId, S.TermId, S.SessionType, S.SessionReference, S.SessionDescription,
			VD.Name VenueName, VD.Region VenueRegion, VD.Address1 VenueAddress1, VD.Address2 VenueAddress2, VD.Town VenueTown, VD.County VenueCounty, VD.Postcode VenuePostcode, SCH.ScheduleDays, SCH.ScheduleStartTimes, SCH.ScheduleEndTimes, OC.NoOfSessions,
            PD.DocId, Doc.SyncGuid, S.SessionId EventDocId, PD.Code, PD.[Name], PD.[Description], PD.ProductType, PD.Category ProductCategory, PD.TaxCode, PD.IsOnSale, PD.Tag, 
            PD.[Location] ImageSrc, PD.NoShippingNeeded, PD.MaxPurchasableQuantity, PD.MinPurchasableQuantity, (S.Capacity - ISNULL(STAT.TotalBookedCount, 0))  AvailableQuantity, 
            PD.ProductReference Reference, PD.OwningEntityType EventBookingType, PD.PricingType, PD.ProductStartTime, PD.ProductEndTime, PD.IsMaster, PD.SurveySchemeId, PD.SurveyInstanceId,
            ISNULL(PD.ShowOnValidationFail, 0) ShowOnValidationFail, PD.SalesTaxCountry, PD.SalesTaxDescription, PD.SalesTaxPercentage, PD.IsSalesTaxIncludedInPrice,
            PD.PreventMultipleBooking,PD.ValidationFailureReason, ISNULL(PD.Unitprice, 0) Unitprice, PD.PriceOption,PD.FromPrice MinPrice,PD.ToPrice MaxPrice, PD.AlternateDisplayCurrency,
            S.TimeZoneId Timezone, PD.OwningEntityType, PD.OwningEntityid,

            PD.TicketCategory, 
            PD.SubTicketLinkConfig, 
            PD.GroupTicketInfo,
            PD.IsSubTicket, 
            0 SubTicketDocId, 
            '' GroupName, 
            NUll BaseTicketDocId,
            NULL ScheduleDate, 
            NULL ScheduleEndDate, 
            0 IsFullProgram, 
            null RecurringTime, 
            0 IsMandatory, 
            0 RecurringRowId, 
            0 NumberOfDays, 
            PD.IsPreventMultipleGroupBooking,

            ISNULL(CD.ClubName,'Ngb') EntityName, ISNULL(CD.ClubType, 'Ngb') EntityType, CD.[Location] EntityImg, CD.ClubName, CD.[Location] ClubLogo,

            DATEADD(second,y.gm_offset,ISNULL(PD.ProductStartDate, '1901-01-01 00:00:00')) StartDate,
            PD.ProductStartTime StartTime,
            Y.abbreviation StartTimeZone, TZ.zone_id StartTimeZoneId,
            DATEADD(second,Z.gm_offset,PD.ProductEndDate) EndDate,
            PD.Productendtime EndTime,
            Z.abbreviation EndTimeZone, TZ.zone_id EndTimeZoneId,

            ISNULL(BOOK.NoOfBooking,0) NoOfBooking, PD.IsInstallmentEnabled,

            FORMAT(ISNULL(PD.Unitprice, 0), 'N2') DisplayPrice, PD.[Sequence],
            (SELECT TOP 1 [Value] FROM SystemSettings WHERE ItemKey = 'SYSTEM.CURRENCY.DEFAULTCURRENCY') Currency,

            --cast((select count(*) from Products_Waitlist where docid= PD.DocId and Reserved=1 and [Status] not in ('Cancelled','Completed','Transferred')) AS DECIMAL(14,4)) ReservedQuantity,
            0 ReservedQuantity,
            (select SalesTaxId from MerchantProfile_Default where DocId=(select top 1 MarchentProfileId from Products_Paymentrecipients where Docid=PD.DocId)) SalesTaxId,

            --CASE WHEN ( (PD.Productenddate < @UtcDate and iif((ISNULL((SELECT top 1 1 FROM Products_Waitlist W where W.DocId =  PD.docid and W.Entityid = @MemberId 
            --AND W.Reserved = 1 AND W.[Status] NOT IN ('Cancelled','Completed','Transferred')),0)<>1),0,1) = 1 /*and ISNULL(PD.Overridebookingdate,0) = 1*/  ) 
            --or   ( (PD.ProductStartDate IS NULL OR @UtcDate>=PD.ProductStartDate ) and  PD.Productenddate >= @UtcDate )) THEN 1
            --ELSE 0 END ProductAvailability,
            1 ProductAvailability,

            CASE WHEN ((S.Capacity - ISNULL(STAT.TotalBookedCount, 0)) > 0) THEN 0 ELSE 1 END ProductAwaitable,
            T.StartDate TermStartDate, T.EndDate TermEndDate,
            ISNULL(STAT.TotalBookedCount, 0) TotalBookedCount, ISNULL(STAT.BookedCount, 0) BookedCount, ISNULL(STAT.TrialCount, 0) TrialCount
            FROM CLASS_PRODS CP
            INNER JOIN CLASS_SESSIONS S ON S.SessionId = CP.SessionId
            INNER JOIN SESSION_OPTIONS O ON O.SessionId = CP.SessionId
            INNER JOIN SESSION_SCHEDULES SCH ON SCH.SessionId = CP.SessionId
            INNER JOIN Products_Default PD ON PD.DocId = CP.ProductId
            INNER JOIN [Document] Doc ON Doc.DocId = PD.DocId
            LEFT JOIN CD ON CD.DocId = PD.OwningEntityid
            LEFT JOIN BOOK ON BOOK.SessionId = S.SessionId
            LEFT JOIN CTE_BOOK_STAT STAT ON STAT.SessionId = S.SessionId
            LEFT JOIN Venue_Default VD ON VD.DocId = S.VenueId
            LEFT JOIN OCCURENCE OC ON OC.SessionId = S.SessionId
            LEFT JOIN JustGoBookingClassTerm T ON T.ClassTermId = S.TermId
            --Need to change on new requirements
            LEFT JOIN Timezone_Zone TZ ON TZ.zone_id = S.TimeZoneId
            OUTER APPLY
            (SELECT TOP 1 gm_offset,abbreviation FROM Timezone WHERE time_start <=  CAST(DATEDIFF(HOUR,'1970-01-01 00:00:00', ISNULL(PD.ProductStartDate, '1901-01-01 00:00:00')) AS BIGINT)*60*60
            AND zone_id=S.TimeZoneId ORDER BY time_start DESC) AS Y
            OUTER APPLY
            (SELECT TOP 1 gm_offset,abbreviation FROM Timezone WHERE time_start <= CAST(DATEDIFF(HOUR,'1970-01-01 00:00:00', PD.ProductEndDate) AS BIGINT)*60*60
            AND zone_id=S.TimeZoneId ORDER BY time_start DESC) AS Z
            --Need to change on new requirements

            WHERE PD.isOnSale=1
            AND PD.Productenddate >= @UtcDate AND @UtcDate >= PD.ProductStartDate
            ORDER BY PD.DocId ASC
            OPTION(OPTIMIZE FOR UNKNOWN );";
        }

    }
}
