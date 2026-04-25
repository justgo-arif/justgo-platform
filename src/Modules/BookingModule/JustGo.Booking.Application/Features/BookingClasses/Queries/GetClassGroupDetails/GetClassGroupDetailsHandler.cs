using System.Data;
using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;

namespace JustGo.Booking.Application.Features.BookingClasses.Queries.GetClassGroupDetails;

public class GetClassGroupDetailsHandler : IRequestHandler<GetClassGroupDetailsQuery, BookingClassGroupDetailsDto?>
{
    private readonly IReadRepositoryFactory _readRepository;
    public GetClassGroupDetailsHandler(IReadRepositoryFactory readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<BookingClassGroupDetailsDto?> Handle(GetClassGroupDetailsQuery request, CancellationToken cancellationToken)
    {
        return await GetClassGroupDetailsAsync(request, cancellationToken);
    }

    private async Task<BookingClassGroupDetailsDto?> GetClassGroupDetailsAsync(GetClassGroupDetailsQuery request, CancellationToken cancellationToken)
    {
        var queryParameters = new DynamicParameters();
        queryParameters.Add("@ClassGuid", request.ClassGuid.ToString(), DbType.String, size: 100);

        var sql = $"""
            SELECT TOP 1 C.ClassId ClassGroupId, C.OwningEntitySyncGuid ClubGuid, CC.CategoryGuid
            FROM JustGoBookingClass C
            INNER JOIN JustGoBookingClassCategory CAT ON CAT.ClassId = C.ClassId AND ISNULL(CAT.IsDeleted, 0) = 0 AND CAT.CategoryType = 1
            INNER JOIN JustGoBookingCategory CC ON CC.CategoryId = CAT.CategoryId AND CC.ParentId = -1 
            WHERE ClassGuid = @ClassGuid AND C.IsDeleted != 1
            ;
            """;
        return await _readRepository.GetLazyRepository<BookingClassGroupDetailsDto>().Value.QueryFirstAsync<BookingClassGroupDetailsDto>(sql, queryParameters, null, "text", cancellationToken);
    }
}
