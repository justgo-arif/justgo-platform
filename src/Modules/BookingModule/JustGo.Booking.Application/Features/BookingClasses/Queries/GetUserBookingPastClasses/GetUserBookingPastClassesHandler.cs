using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Booking.Domain.Entities;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetUserBookingPastClasses;

public class GetUserBookingPastClassesHandler : IRequestHandler<GetUserBookingPastClassesQuery, List<MemberClassDto>>
{
    private readonly IReadRepositoryFactory _readRepository;

    public GetUserBookingPastClassesHandler(IReadRepositoryFactory readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<List<MemberClassDto>> Handle(GetUserBookingPastClassesQuery request, CancellationToken cancellationToken = default)
    {
        var data = await GetPastMemberClassesAsync(request, cancellationToken);

        return data.Select(d => MapToDto(d)).ToList();
    }

    private async Task<List<MemberClass>> GetPastMemberClassesAsync(GetUserBookingPastClassesQuery request, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        queryParameters.Add("UserGuid", request.UserGuid);
        queryParameters.Add("ClassGuid", request.ClassGuid);

        var sql = $"""
                   DECLARE @MemberDocId INT = (SELECT TOP 1 MemberDocId FROM [User] WHERE UserSyncId = @UserGuid);
                   DECLARE @SessionId INT = (SELECT TOP 1 SessionId FROM JustGoBookingClassSession WHERE ClassSessionGuid = @ClassGuid);

                   WITH AttendeeInfo AS (
                       SELECT A.SessionId, A.AttendeeId, AD.AttendeeDetailsId, AD.OccurenceId, AD.AttendeeType, AD.AttendeePaymentId, O.StartDate, O.EndDate
                       FROM JustGoBookingAttendee A 
                       INNER JOIN JustGoBookingAttendeeDetails AD ON AD.AttendeeId = A.AttendeeId AND AD.AttendeeDetailsStatus = 1 --(1 = Active, 2 = Expire, 3 = Cancel, 4 = Transfer)
                       INNER JOIN JustGoBookingScheduleOccurrence O ON O.OccurrenceId = AD.OccurenceId AND O.IsDeleted = 0
                       WHERE A.EntityDocId = @MemberDocId AND A.SessionId = @SessionId
                       AND O.EndDate < GETUTCDATE()
                   ),
                   Classes AS (
                       SELECT DISTINCT C.ClassId
                       FROM AttendeeInfo A 
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
                           INNER JOIN AttendeeInfo A ON A.SessionId = C.EntityId AND C.IsDeleted = 0
                       ) A
                       GROUP BY A.SessionId
                   ),
                   PurchaseInfo AS (
                       SELECT A.SessionId, A.OccurenceId, A.AttendeePaymentId, AP.ProductId, P.ProductType, PRI.Gross BookingAmount
                       FROM AttendeeInfo A
                       INNER JOIN JustGoBookingAttendeePayment AP ON AP.AttendeeId = A.AttendeeId AND AP.AttendeePaymentId = A.AttendeePaymentId
                       INNER JOIN JustGoBookingClassSessionProduct P ON P.SessionId = A.SessionId AND P.ProductId = AP.ProductId
                       INNER JOIN PaymentReceipts_Default PR ON PR.DocId = AP.PaymentId
                       INNER JOIN PaymentReceipts_Items PRI ON PRI.DocId = PR.DocId AND PRI.Productid = AP.ProductId AND PRI.Forentityid = @MemberDocId
                   )
                   SELECT C.[Name] ClassGroupName, S.[Name] ClassName, S.ClassSessionGuid ClassGuid, VD.[Name] VenueName, CO.Coaches, A.SessionId, A.AttendeeId, A.AttendeeDetailsId, A.OccurenceId, A.AttendeePaymentId, A.StartDate, A.EndDate,
                   P.BookingAmount, P.ProductType, IMGS.ClassImages
                   FROM AttendeeInfo A
                   INNER JOIN JustGoBookingClassSession S ON S.SessionId = A.SessionId
                   INNER JOIN JustGoBookingClass C ON C.ClassId = S.ClassId
                   INNER JOIN Venue_Default VD ON VD.DocId = S.VenueId
                   INNER JOIN PurchaseInfo P ON P.SessionId = A.SessionId AND P.OccurenceId = A.OccurenceId
                   LEFT JOIN Coaches CO ON CO.SessionId = S.SessionId
                   LEFT JOIN IMGS ON IMGS.ClassId = C.ClassId
                   ORDER BY A.EndDate DESC;
                   """;

        return (await _readRepository.GetLazyRepository<MemberClass>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
    }

    private static MemberClassDto MapToDto(MemberClass memberClass)
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
            ClassImages = classImagesArray
        };
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
