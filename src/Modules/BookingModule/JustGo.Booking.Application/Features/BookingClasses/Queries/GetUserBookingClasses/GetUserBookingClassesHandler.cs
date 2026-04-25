using Dapper;
using JustGo.Authentication.Helper.Paginations.Keyset;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Booking.Domain.Entities;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetUserBookingClasses;

public class GetUserBookingClassesHandler : IRequestHandler<GetUserBookingClassesQuery, KeysetPagedResult<MemberClassDto>>
{
    private readonly IReadRepositoryFactory _readRepository;

    public GetUserBookingClassesHandler(IReadRepositoryFactory readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<KeysetPagedResult<MemberClassDto>> Handle(GetUserBookingClassesQuery request, CancellationToken cancellationToken = default)
    {
        List<MemberClass> data;
        if (request.IsPast)
        {
            data = await GetPastMemberClassesAsync(request, cancellationToken);
        }
        else
        {
            data = await GetPresentMemberClassesAsync(request, cancellationToken);
        }

        var hasMore = data.Count > request.NumberOfRow;
        if (hasMore)
            data.RemoveAt(data.Count - 1);

        var classes = data.Select(d => MapToDto(d, request.IsPast)).ToList();

        return new KeysetPagedResult<MemberClassDto>()
        {
            Items = classes,
            TotalCount = request.TotalRows is > 0 ? request.TotalRows.Value : data.FirstOrDefault()?.TotalRows ?? 0,
            HasMore = hasMore,
            LastSeenId = data.LastOrDefault()?.RowNumber ?? 0
        };
    }

    private async Task<List<MemberClass>> GetPresentMemberClassesAsync(GetUserBookingClassesQuery request, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        queryParameters.Add("UserGuid", request.UserGuid);
        queryParameters.Add("LastSeenId", request.LastSeenId ?? 0);
        queryParameters.Add("NumberOfRow", request.NumberOfRow + 1);

        string totalRowsQuery = (request.TotalRows ?? 0) > 0 ? $"{request.TotalRows}" : "COUNT(1) OVER()";

        var sql = $"""
                   DECLARE @MemberDocId INT = (SELECT TOP 1 MemberDocId FROM [User] WHERE UserSyncId = @UserGuid);
                   DECLARE @DefaultLogo NVARCHAR(MAX) = (SELECT TOP 1  Value FROM systemsettings  WHERE itemkey = 'ORGANISATION.LOGO');
                   DECLARE @organisationname NVARCHAR(MAX)=(SELECT TOP 1 [Value] FROM SystemSettings WHERE ItemKey ='ORGANISATION.NAME');
                   WITH AttendeeInfo AS (
                   	    SELECT A.SessionId, A.AttendeeId, AD.AttendeeDetailsId, AD.OccurenceId, AD.AttendeeType, AD.AttendeePaymentId, O.StartDate, O.EndDate, ROW_NUMBER() OVER(ORDER BY O.EndDate ASC) RowNumber
                   	    FROM JustGoBookingAttendee A 
                   	    INNER JOIN JustGoBookingAttendeeDetails AD ON AD.AttendeeId = A.AttendeeId AND AD.AttendeeDetailsStatus = 1 --(1 = Active, 2 = Expire, 3 = Cancel, 4 = Transfer)
                   	    INNER JOIN JustGoBookingScheduleOccurrence O ON O.OccurrenceId = AD.OccurenceId AND O.IsDeleted = 0
                   	    WHERE A.EntityDocId = @MemberDocId
                   	    AND O.EndDate >= GETUTCDATE()
                   ),
                   PagedAttendee AS (
                       SELECT TOP (@NumberOfRow) A.SessionId, A.AttendeeId, A.AttendeeDetailsId, A.OccurenceId, A.AttendeeType, A.AttendeePaymentId, A.StartDate, A.EndDate, A.RowNumber, {totalRowsQuery} TotalRows
                       FROM AttendeeInfo A  
                       WHERE RowNumber > @LastSeenId
                       ORDER BY RowNumber
                   ),
                   Classes AS (
                   	    SELECT DISTINCT C.ClassId
                   	    FROM PagedAttendee A 
                   	    INNER JOIN JustGoBookingClassSession S ON S.SessionId = A.SessionId
                   	    INNER JOIN JustGoBookingClass C ON C.ClassId = S.ClassId
                   ),
                   IMGS AS (
                       SELECT A.ClassId, STRING_AGG(CONCAT('/store/downloadpublic?f=', A.[Name], '&t=justgobookingattachment&p=', A.EntityId, '&p1=', A.EntityTypeId), '|') ClassImages	
                       FROM (
                       SELECT C.ClassId, A.[Name], A.EntityId, A.EntityTypeId
                       FROM JustGoBookingAttachment A
                       INNER JOIN Classes C ON C.ClassId = A.EntityId AND A.EntityTypeId = 1 AND A.IsDeleted = 0
                       GROUP BY C.ClassId, A.[Name], A.EntityId, A.EntityTypeId
                       ) A
                       GROUP BY A.ClassId
                   ),
                   Coaches AS (
                   	    SELECT A.SessionId, STRING_AGG(A.FirstName + ' ' +  A.LastName, ',' ) Coaches
                   	    FROM (
                   		    SELECT DISTINCT A.SessionId, C.FirstName, C.LastName
                   		    FROM JustGoBookingContact C
                   		    INNER JOIN PagedAttendee A ON A.SessionId = C.EntityId AND C.IsDeleted = 0
                   	    ) A
                   	    GROUP BY A.SessionId
                   ),
                   PurchaseInfo AS (
                   	    SELECT A.SessionId, A.OccurenceId, A.AttendeePaymentId, AP.ProductId, P.ProductType, PRI.Gross BookingAmount
                   	    FROM PagedAttendee A
                   	    INNER JOIN JustGoBookingAttendeePayment AP ON AP.AttendeeId = A.AttendeeId AND AP.AttendeePaymentId = A.AttendeePaymentId
                   	    INNER JOIN JustGoBookingClassSessionProduct P ON P.SessionId = A.SessionId AND P.ProductId = AP.ProductId
                   	    INNER JOIN PaymentReceipts_Default PR ON PR.DocId = AP.PaymentId
                   	    INNER JOIN PaymentReceipts_Items PRI ON PRI.DocId = PR.DocId AND PRI.Productid = AP.ProductId AND PRI.Forentityid = @MemberDocId
                   )
                   SELECT C.[Name] ClassGroupName, S.[Name] ClassName, S.ClassSessionGuid ClassGuid, VD.[Name] VenueName, CO.Coaches, A.SessionId, A.AttendeeId, A.AttendeeDetailsId, A.OccurenceId, A.AttendeePaymentId, A.StartDate, A.EndDate,
                   P.BookingAmount, P.ProductType, IMGS.ClassImages, A.TotalRows, A.RowNumber
                   ,CASE 
                       WHEN C.OwningEntityid = 0 
                       THEN @organisationname 
                       ELSE cd.ClubName 
                   END as ClubName
                   ,CASE 
                      WHEN C.OwningEntityId = 0  THEN CONCAT( 'Store/Download?f=', @DefaultLogo ,'&t=OrganizationLogo' )
                       WHEN cd.Location = 'Virtual' OR ISNULL(cd.Location,'') = '' THEN ''  
                       ELSE Concat('store/download?f=', cd.Location, '&t=repo&p=', cd.DocId,'&p1=&p2=2')  
                   END AS ClubImageUrl
                   FROM PagedAttendee A
                   INNER JOIN JustGoBookingClassSession S ON S.SessionId = A.SessionId
                   INNER JOIN JustGoBookingClass C ON C.ClassId = S.ClassId
                   INNER JOIN Venue_Default VD ON VD.DocId = S.VenueId
                   INNER JOIN PurchaseInfo P ON P.SessionId = A.SessionId AND P.OccurenceId = A.OccurenceId
                   LEFT JOIN Coaches CO ON CO.SessionId = S.SessionId
                   LEFT JOIN IMGS ON IMGS.ClassId = C.ClassId
                   LEFT JOIN Clubs_Default cd ON cd.DocId = C.OwningEntityid
                   ORDER BY A.RowNumber;
                   """;

        return (await _readRepository.GetLazyRepository<MemberClass>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
    }

    private async Task<List<MemberClass>> GetPastMemberClassesAsync(GetUserBookingClassesQuery request, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        queryParameters.Add("UserGuid", request.UserGuid);
        queryParameters.Add("LastSeenId", request.LastSeenId ?? 0);
        queryParameters.Add("NumberOfRow", request.NumberOfRow + 1);

        string totalRowsQuery = (request.TotalRows ?? 0) > 0 ? $"{request.TotalRows}" : "COUNT(1) OVER()";

        var sql = $"""
                   DECLARE @MemberDocId INT = (SELECT TOP 1 MemberDocId FROM [User] WHERE UserSyncId = @UserGuid);
                   DECLARE @DefaultLogo NVARCHAR(MAX) = (SELECT TOP 1  Value FROM systemsettings  WHERE itemkey = 'ORGANISATION.LOGO');
                   DECLARE @organisationname NVARCHAR(MAX)=(SELECT TOP 1 [Value] FROM SystemSettings WHERE ItemKey ='ORGANISATION.NAME');
                   WITH AttendeeInfo AS (
                       SELECT A.SessionId, A.AttendeeId, AD.AttendeeDetailsId, AD.OccurenceId, AD.AttendeeType, AD.AttendeePaymentId, O.StartDate, O.EndDate, ROW_NUMBER() OVER(ORDER BY O.EndDate DESC) RowNumber
                       FROM JustGoBookingAttendee A 
                       INNER JOIN JustGoBookingAttendeeDetails AD ON AD.AttendeeId = A.AttendeeId AND AD.AttendeeDetailsStatus = 1 --(1 = Active, 2 = Expire, 3 = Cancel, 4 = Transfer)
                       INNER JOIN JustGoBookingScheduleOccurrence O ON O.OccurrenceId = AD.OccurenceId AND O.IsDeleted = 0
                       WHERE A.EntityDocId = @MemberDocId
                       AND O.EndDate < GETUTCDATE()
                   ),
                   PagedAttendee AS (
                       SELECT TOP (50) A.SessionId, A.AttendeeId, A.NoOfClasses, A.RowNumber, {totalRowsQuery} TotalRows
                       FROM (
                           SELECT A.SessionId, A.AttendeeId, MIN(A.RowNumber) RowNumber, COUNT(1) NoOfClasses
                           FROM AttendeeInfo A
                           GROUP BY A.SessionId, A.AttendeeId
                       ) A
                       WHERE A.RowNumber > 0
                       ORDER BY A.RowNumber
                   ),
                   AttendeeDates AS (
                       SELECT A.SessionId, A.AttendeeId, MIN(AI.StartDate) StartDate, MAX(AI.EndDate) EndDate
                       FROM PagedAttendee A
                       INNER JOIN AttendeeInfo AI ON AI.SessionId = A.SessionId --AND A.AttendeeId = AI.AttendeeId
                       GROUP BY A.SessionId, A.AttendeeId
                   ),
                   Classes AS (
                       SELECT DISTINCT C.ClassId
                       FROM PagedAttendee A 
                       INNER JOIN JustGoBookingClassSession S ON S.SessionId = A.SessionId
                       INNER JOIN JustGoBookingClass C ON C.ClassId = S.ClassId
                   ),
                   IMGS AS (
                       SELECT A.ClassId, STRING_AGG(CONCAT('/store/downloadpublic?f=', A.[Name], '&t=justgobookingattachment&p=', A.EntityId, '&p1=', A.EntityTypeId), '|') ClassImages	
                       FROM (
                       SELECT C.ClassId, A.[Name], A.EntityId, A.EntityTypeId
                       FROM JustGoBookingAttachment A
                       INNER JOIN Classes C ON C.ClassId = A.EntityId AND A.EntityTypeId = 1 AND A.IsDeleted = 0
                       GROUP BY C.ClassId, A.[Name], A.EntityId, A.EntityTypeId
                       ) A
                       GROUP BY A.ClassId
                   ),
                   Coaches AS (
                       SELECT A.SessionId, STRING_AGG(A.FirstName + '|' +  A.LastName, ',' ) Coaches
                       FROM (
                           SELECT DISTINCT A.SessionId, C.FirstName, C.LastName
                           FROM JustGoBookingContact C
                           INNER JOIN PagedAttendee A ON A.SessionId = C.EntityId AND C.IsDeleted = 0
                       ) A
                       GROUP BY A.SessionId
                   )
                   SELECT C.[Name] ClassGroupName, S.[Name] ClassName, S.ClassSessionGuid ClassGuid, VD.[Name] VenueName, CO.Coaches, A.SessionId, A.AttendeeId, D.StartDate, D.EndDate,
                   IMGS.ClassImages, A.NoOfClasses, A.TotalRows, A.RowNumber
                   ,CASE 
                       WHEN C.OwningEntityid = 0 
                       THEN @organisationname 
                       ELSE cd.ClubName 
                   END as ClubName
                   ,CASE 
                      WHEN C.OwningEntityId = 0  THEN CONCAT( 'Store/Download?f=', @DefaultLogo ,'&t=OrganizationLogo' )
                       WHEN cd.Location = 'Virtual' OR ISNULL(cd.Location,'') = '' THEN ''  
                       ELSE Concat('store/download?f=', cd.Location, '&t=repo&p=', cd.DocId,'&p1=&p2=2')  
                   END AS ClubImageUrl
                   FROM PagedAttendee A
                   INNER JOIN JustGoBookingClassSession S ON S.SessionId = A.SessionId
                   INNER JOIN JustGoBookingClass C ON C.ClassId = S.ClassId
                   INNER JOIN Venue_Default VD ON VD.DocId = S.VenueId
                   INNER JOIN AttendeeDates D ON D.SessionId = A.SessionId
                   LEFT JOIN Coaches CO ON CO.SessionId = S.SessionId
                   LEFT JOIN IMGS ON IMGS.ClassId = C.ClassId
                   LEFT JOIN Clubs_Default cd ON cd.DocId = C.OwningEntityid
                   ORDER BY A.RowNumber;
                   """;

        return (await _readRepository.GetLazyRepository<MemberClass>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
    }

    private static MemberClassDto MapToDto(MemberClass memberClass, bool isPast)
    {
        string[] coaches = Array.Empty<string>();
        if (!string.IsNullOrWhiteSpace(memberClass.Coaches))
        {
            coaches = memberClass.Coaches
                                      .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(s => s.Trim())
                                      .ToArray();
        }

        string[] classImagesArray = Array.Empty<string>();
        if (!string.IsNullOrWhiteSpace(memberClass.ClassImages))
        {
            classImagesArray = memberClass.ClassImages
                                           .Split('|', StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => s.Trim())
                                           .ToArray();
        }

        if (isPast)
        {
            return new MemberClassDto
            {
                ClassGroupName = memberClass.ClassGroupName,
                ClassName = memberClass.ClassName,
                ClassGuid = memberClass.ClassGuid,
                VenueName = memberClass.VenueName,
                StartDate = DateOnly.FromDateTime(memberClass.StartDate),
                EndDate = DateOnly.FromDateTime(memberClass.EndDate),
                StartTime = TimeOnly.FromDateTime(memberClass.StartDate),
                EndTime = TimeOnly.FromDateTime(memberClass.EndDate),
                NoOfClasses = memberClass.NoOfClasses,
                Coaches = coaches,
                ClassImages = classImagesArray,
                ClubName = memberClass.ClubName,
                ClubImageUrl = memberClass.ClubImageUrl,
            };
        }
        else
        {
            return new MemberClassDto
            {
                ClassGroupName = memberClass.ClassGroupName,
                ClassName = memberClass.ClassName,
                ClassGuid = memberClass.ClassGuid,
                VenueName = memberClass.VenueName,
                StartDate = DateOnly.FromDateTime(memberClass.StartDate),
                EndDate = DateOnly.FromDateTime(memberClass.EndDate),
                StartTime = TimeOnly.FromDateTime(memberClass.StartDate),
                EndTime = TimeOnly.FromDateTime(memberClass.EndDate),
                BookingAmount = memberClass.BookingAmount,
                ProductType = GetProductType(memberClass.ProductType),
                Coaches = coaches,
                ClassImages = classImagesArray,
                ClubName = memberClass.ClubName,
                ClubImageUrl = memberClass.ClubImageUrl,
            };
        }
    }

    private static string GetProductType(int productType)
    {
        return productType switch
        {
            1 => "one-off",
            2 => "trial",
            3 => "payg",
            4 => "monthly",
            _ => ""
        };
    }

}
