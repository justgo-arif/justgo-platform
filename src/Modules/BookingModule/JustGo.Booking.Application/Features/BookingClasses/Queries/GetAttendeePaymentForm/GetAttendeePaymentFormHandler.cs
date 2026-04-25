using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetAttendeePaymentForm;

public class GetAttendeePaymentFormHandler : IRequestHandler<GetAttendeePaymentFormQuery, List<AttendeePaymentFormDto>>
{
    private readonly IReadRepositoryFactory _readRepository;
    private readonly IUtilityService _utilityService;
    public GetAttendeePaymentFormHandler(IReadRepositoryFactory readRepository, IUtilityService utilityService)
    {
        _readRepository = readRepository;
        _utilityService = utilityService;
    }

    public async Task<List<AttendeePaymentFormDto>> Handle(GetAttendeePaymentFormQuery request, CancellationToken cancellationToken)
    {
        return await GetFormResultAsync(request, cancellationToken);
    }

    private async Task<List<AttendeePaymentFormDto>> GetFormResultAsync(GetAttendeePaymentFormQuery request, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        queryParameters.Add("AttendeeId", request.AttendeeId);

        var sql = $"""
            DECLARE 
            @FormName VARCHAR(1000),
            @FormId INT;

            Select @FormName = JSON_VALUE(DCI.Config, '$.tabName'), @FormId = JSON_VALUE(DCI.Config, '$."$dataFieldInfo".compId')
            FROM JustGoBookingAttendee A
            INNER JOIN JustGoBookingClassSession S ON S.SessionId = A.SessionId AND ISNULL(S.IsDeleted, 0) = 0
            INNER JOIN JustGoBookingClassSessionProduct SP ON SP.SessionId = A.SessionId and SP.ProductType=1  AND ISNULL(SP.IsDeleted, 0) = 0
            INNER JOIN Products_Default pd on sp.ProductId = pd.DocId
            INNER JOIN Products_Datacaptureitems dci  on dci.DocId = pd.DocId
            where AttendeeId = @AttendeeId;

            DECLARE @Ids VARCHAR(1000) = (
                SELECT STRING_AGG(Id, ',') WITHIN GROUP (ORDER BY ParentId ASC, [Sequence] ASC) Ids
                FROM
                (
                    SELECT EF.Id, UI.[Sequence], UI.ParentId
                    FROM EntityExtensionField EF
                    INNER JOIN EntityExtensionUI UI ON UI.FieldId = EF.Id
                    WHERE Id IN (SELECT FieldId FROM EntityExtensionUI WHERE ParentId IN  
                    (SELECT ItemId FROM EntityExtensionUI WHERE Class = 'MA_TabItem' 
                    AND ItemId = @FormId
                    ))
                ) F
            )
            ----Select @Ids
            EXEC GetFieldMgtDetails_Fieldset @Ids, @AttendeeId
            ;
            """;

        return (await _readRepository.GetLazyRepository<AttendeePaymentFormDto>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();
    }
}
