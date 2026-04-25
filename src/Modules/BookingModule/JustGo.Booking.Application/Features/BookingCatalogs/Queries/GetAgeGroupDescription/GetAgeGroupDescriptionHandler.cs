using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingCatalogDTOs;

namespace JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetAgeGroupDescription;

public class GetAgeGroupDescriptionHandler : IRequestHandler<GetAgeGroupDescriptionQuery, DescriptionDto?>
{
    private readonly IReadRepositoryFactory _readRepository;
    public GetAgeGroupDescriptionHandler(IReadRepositoryFactory readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<DescriptionDto?> Handle(GetAgeGroupDescriptionQuery request, CancellationToken cancellationToken)
    {
        string cacheKey = $"justgobooking:age-group-description:{request.Id}";
        const string sql = """
                           SELECT G.[NAME], G.[Description] FROM JustGoBookingAgeGroup G
                           WHERE G.Id = @Id;
                           """;

        var queryParameters = new DynamicParameters();
        queryParameters.Add("@Id", request.Id);
        return await _readRepository.GetLazyRepository<DescriptionDto>().Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
    }
}