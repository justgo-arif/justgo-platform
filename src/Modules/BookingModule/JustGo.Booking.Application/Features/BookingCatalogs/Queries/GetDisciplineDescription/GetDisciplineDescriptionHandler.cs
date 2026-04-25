using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingCatalogDTOs;

namespace JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetDisciplineDescription;

public class GetDisciplineDescriptionHandler : IRequestHandler<GetDisciplineDescriptionQuery, DescriptionDto?>
{
    private readonly IReadRepositoryFactory _readRepository;
    public GetDisciplineDescriptionHandler(IReadRepositoryFactory readRepository)
    {
        _readRepository = readRepository;
    }

    public async Task<DescriptionDto?> Handle(GetDisciplineDescriptionQuery request, CancellationToken cancellationToken)
    {
        string cacheKey = $"justgobooking:discipline-description:{request.Id}";
        const string sql = """
                           SELECT C.[NAME], C.[Description] FROM JustGoBookingCategory C
                           WHERE C.CategoryGuid = @Id;
                           """;

        var queryParameters = new DynamicParameters();
        queryParameters.Add("@Id", request.Id);

        return await _readRepository.GetLazyRepository<DescriptionDto>().Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
    }
}