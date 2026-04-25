using System.Data;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetAttendees
{
    internal class GetAttendeesHandler : IRequestHandler<GetAttendeesQuery, List<BookingAttendeeDto>>
    {
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;
        public GetAttendeesHandler(IReadRepositoryFactory readRepository, IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _utilityService = utilityService;
        }

        public async Task<List<BookingAttendeeDto>> Handle(GetAttendeesQuery request, CancellationToken cancellationToken)
        {
            var currentUser = await _utilityService.GetCurrentUser(cancellationToken);
            return await GetAttendeesAsync(request, currentUser, cancellationToken);
        }

        private async Task<List<BookingAttendeeDto>> GetAttendeesAsync(GetAttendeesQuery request, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            var queryParameters = new DynamicParameters();
            queryParameters.Add("SessionGuid", request.SessionGuid.ToString(), DbType.String, size: 100);
            queryParameters.Add("MemberDocId", currentUser.MemberDocId);
            queryParameters.Add("UserId", currentUser.UserId);

            var sql = $"""
            DECLARE @BaseUrl Varchar(100) = (SELECT [Value] FROM SystemSettings Where ItemKey = 'SYSTEM.SITEADDRESS')
            SET @BaseUrl = IIF(RIGHT(@BaseUrl, 1) = '/', LEFT(@BaseUrl, LEN(@BaseUrl) - 1), @BaseUrl);

            --DECLARE @MemberDocId int = 87571 , @UserId int = 1;
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
                SELECT SessionId, ProductId 
                FROM JustGoBookingClassSessionProduct P 
                WHERE SessionId = @SessionId
            ),
            ATTENDEE AS (
                SELECT A.AttendeeId, A.EntityDocId
            	FROM JustGoBookingAttendee A 
            	INNER JOIN ALL_PROD P ON P.SessionId = A.SessionId
            	INNER JOIN JustGoBookingAttendeeDetails AD ON AD.AttendeeId = A.AttendeeId --AND AD.AttendeeDetailsStatus = 1 --(1 = Active, 2 = Expire, 3 = Cancel, 4 = Transfer)
            	INNER JOIN JustGoBookingAttendeePayment AP ON AP.AttendeeId = A.AttendeeId AND AP.ProductId = P.ProductId
                LEFT JOIN FAMILY FLM ON FLM.Entityid = A.EntityDocId
                WHERE (FLM.Entityid IS NOT NULL OR AP.Bookingentityid = @MemberDocId)
            	GROUP BY A.AttendeeId, A.EntityDocId
            )
            Select CONCAT(ISNULL(U.FirstName, ''), ' ', ISNULL(U.LastName, '')) FullName,
            IIF(ISNULL(U.ProfilePicURL, '') = '', '', CONCAT(@BaseUrl, '/store/downloadpublic?f=', U.ProfilePicURL, '&t=user&p=', U.UserId)) AS ImageUrl
            from ATTENDEE A
            INNER JOIN [User] U ON U.MemberDocId = A.EntityDocId
            ;
            """;

            return (await _readRepository.GetLazyRepository<BookingAttendeeDto>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
        }
    }
}
