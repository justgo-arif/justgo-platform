using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Booking.Domain.Entities;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetAttendeePayments;

public class GetAttendeePaymentsHandler : IRequestHandler<GetAttendeePaymentsQuery, List<GroupedBookingAttendeePaymentDto>>
{
    private readonly IReadRepositoryFactory _readRepository;
    private readonly IUtilityService _utilityService;
    public GetAttendeePaymentsHandler(IReadRepositoryFactory readRepository, IUtilityService utilityService)
    {
        _readRepository = readRepository;
        _utilityService = utilityService;
    }

    public async Task<List<GroupedBookingAttendeePaymentDto>> Handle(GetAttendeePaymentsQuery request, CancellationToken cancellationToken)
    {
        var currentUser = await _utilityService.GetCurrentUser(cancellationToken);
        var payments = await GetPaymentsAsync(request, currentUser, cancellationToken);

        var groupedPayments = payments
            .GroupBy(p => new { p.PaymentReceiptId, p.PaymentReceiptDocId })
            .Select(g => new GroupedBookingAttendeePaymentDto
            {
                PaymentReceiptId = g.Key.PaymentReceiptId ?? "",
                PaymentReceiptDocId = g.Key.PaymentReceiptDocId,
                TotalAmount = g.ToList().Sum(x => x.BookingAmount),
                TotalTickets = g.ToList().Sum(x => x.NoOfBooking),
                Details = g.Select(p => new BookingAttendeePaymentDetailDto
                {
                    AttendeeId = p.AttendeeId,
                    ProductDocId = p.ProductDocId,
                    MemberDocId = p.MemberDocId,
                    NoOfBooking = p.NoOfBooking,
                    ProductType = p.ProductType,
                    VenueName = p.VenueName,
                    BookingAmount = p.BookingAmount,
                    UserId = p.UserId,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    MID = p.MID,
                    EmailAddress = p.EmailAddress,
                    ImageUrl = p.ImageUrl,
                    Address1 = p.Address1,
                    Address2 = p.Address2,
                    Address3 = p.Address3,
                    PostCode = p.PostCode,
                    Town = p.Town,
                    County = p.County,
                    Country = p.Country,
                    IsFormAvailable = p.IsFormAvailable,
                    AttendeeDetailsStatus = p.AttendeeDetailsStatus
                }).ToList()
            })
            .ToList();
        return groupedPayments;
    }

    private async Task<List<BookingAttendeePayment>> GetPaymentsAsync(GetAttendeePaymentsQuery request, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        queryParameters.Add("SessionGuid", request.SessionGuid);
        queryParameters.Add("MemberDocId", currentUser.MemberDocId);
        queryParameters.Add("UserId", currentUser.UserId);

        var sql = $"""
            DECLARE @BaseUrl Varchar(100) = (SELECT [Value] FROM SystemSettings Where ItemKey = 'SYSTEM.SITEADDRESS')
            SET @BaseUrl = IIF(RIGHT(@BaseUrl, 1) = '/', LEFT(@BaseUrl, LEN(@BaseUrl) - 1), @BaseUrl);

            ----DECLARE @MemberDocId int = 87571 , @UserId int = 1;
            DECLARE @SessionId int = (Select SessionId from JustGoBookingClassSession where ClassSessionGuid = @SessionGuid);

            WITH 
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
                SELECT @MemberDocId
            ),
            ALL_PROD AS (
                SELECT SessionId, ProductId, ProductType
                FROM JustGoBookingClassSessionProduct P 
                WHERE SessionId = @SessionId
            ),
            ATTENDEE AS (
                SELECT DISTINCT A.AttendeeId, AP.ProductId, AP.PaymentId, AP.Bookingentityid
               	FROM JustGoBookingAttendee A 
               	INNER JOIN ALL_PROD P ON P.SessionId = A.SessionId
               	--INNER JOIN JustGoBookingAttendeeDetails AD ON AD.AttendeeId = A.AttendeeId --AND AD.AttendeeDetailsStatus = 1 --(1 = Active, 2 = Expire, 3 = Cancel, 4 = Transfer)
               	INNER JOIN JustGoBookingAttendeePayment AP ON AP.AttendeeId = A.AttendeeId AND AP.ProductId = P.ProductId
            ),
            BOOK AS (
                SELECT A.AttendeeId, AD.ProductId ProductDocId, ALL_PROD.ProductType, A.EntityDocId,
            	PR.Paymentid PaymentReceiptId, PR.DocId PaymentReceiptDocId, SUM(1) NoOfBooking, SUM(PRI.Gross) BookingAmount
                FROM JustGoBookingAttendee A 
                INNER JOIN ATTENDEE AD ON AD.AttendeeId = A.AttendeeId
            	INNER JOIN ALL_PROD ON ALL_PROD.ProductId = AD.ProductId
                LEFT JOIN PaymentReceipts_Default PR ON PR.DocId = AD.PaymentId
                LEFT JOIN PaymentReceipts_Items PRI ON PR.DocId = PRI.DocId AND PRI.Productid = AD.ProductId AND PRI.Forentityid = A.EntityDocId
                LEFT JOIN FAMILY FLM ON FLM.Entityid = A.EntityDocId
                LEFT JOIN JustGoBookingTransferRequest T ON T.DestinationAttendeeId = A.AttendeeId
                WHERE A.SessionId = @SessionId
                AND (FLM.Entityid IS NOT NULL OR AD.Bookingentityid = @memberDocId)
                GROUP BY A.AttendeeId, AD.ProductId, ALL_PROD.ProductType, PR.Paymentid, A.EntityDocId, PR.DocId
            ),
            TRANSFER_BOOK AS (
                SELECT A.AttendeeId, SUM(PRI.Gross) BookingAmount
                FROM JustGoBookingAttendee A 
                INNER JOIN ATTENDEE AD ON AD.AttendeeId = A.AttendeeId
            	INNER JOIN ALL_PROD ON ALL_PROD.ProductId = AD.ProductId
            	INNER JOIN JustGoBookingTransferRequest T ON T.DestinationAttendeeId = A.AttendeeId AND T.DestinationProductDocId = AD.ProductId
                LEFT JOIN PaymentReceipts_Default PR ON PR.DocId = AD.PaymentId
                LEFT JOIN PaymentReceipts_Items PRI ON PR.DocId = PRI.DocId AND PRI.Productid = AD.ProductId AND PRI.Forentityid = A.EntityDocId
                LEFT JOIN FAMILY FLM ON FLM.Entityid = A.EntityDocId
                WHERE A.SessionId = @SessionId
                AND (FLM.Entityid IS NOT NULL OR AD.Bookingentityid = @memberDocId)
                GROUP BY A.AttendeeId, AD.ProductId, PR.Paymentid, A.EntityDocId, PR.DocId
            ),
            FM AS (
                SELECT FS.DocId FROM ExNgbEvent_FieldSet_Master FS INNER JOIN BOOK CB ON CB.AttendeeId = FS.DocId
                UNION
                SELECT FS.DocId FROM ExClubEvent_FieldSet_Master FS INNER JOIN BOOK CB ON CB.AttendeeId = FS.DocId
            )
            SELECT BOOK.AttendeeId, BOOK.ProductDocId, ISNULL(BOOK.PaymentReceiptId,'') PaymentReceiptId, BOOK.EntityDocId MemberDocId, 
            ISNULL(BOOK.PaymentReceiptDocId, 0) PaymentReceiptDocId, ISNULL(BOOK.NoOfBooking, 0) NoOfBooking, BOOK.ProductType, VD.[Name] VenueName,
            IIF(TB.AttendeeId IS NULL, ISNULL(BOOK.BookingAmount, 0), ISNULL(TB.BookingAmount, 0)) BookingAmount,
            U.UserId, U.FirstName, U.LastName, U.MemberId MID, U.EmailAddress, 
            IIF(ISNULL(U.ProfilePicURL, '') = '', '', CONCAT(@BaseUrl, '/store/downloadpublic?f=', U.ProfilePicURL, '&t=user&p=', U.UserId)) ImageUrl,
            U.Address1, U.Address2, U.Address3, U.PostCode, U.Town, U.County, U.Country,
            IIF(FM.DocId IS NULL, 0, 1) IsFormAvailable, JAD.AttendeeDetailsStatus
            FROM BOOK
            INNER JOIN [User] U ON U.MemberDocId = BOOK.EntityDocId
            LEFT JOIN FM ON FM.DocId = BOOK.AttendeeId
            INNER JOIN JustgoBookingClassSession S ON S.SessionId = @SessionId AND ISNULL(S.IsDeleted, 0) = 0
            LEFT JOIN Venue_Default VD ON VD.DocId = S.VenueId
            LEFT JOIN TRANSFER_BOOK TB ON TB.AttendeeId = BOOK.AttendeeId
            OUTER APPLY (
                SELECT TOP 1 AttendeeDetailsStatus 
                FROM JustGoBookingAttendeeDetails 
                WHERE AttendeeId = BOOK.AttendeeId 
                ORDER BY AttendeeDetailsId DESC
            ) JAD
            ;
            """;

        return (await _readRepository.GetLazyRepository<BookingAttendeePayment>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
    }
}

