using AuthModule.Application.DTOs.Lookup;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.Events.Queries.GetEventYears
{
    public class GetEventYearsHandler : IRequestHandler<GetEventYearsQuery, List<SelectListItemDTO<string>>>
    {
        private readonly LazyService<IReadRepository<SelectListItemDTO<string>>> _readRepository;

        public GetEventYearsHandler(LazyService<IReadRepository<SelectListItemDTO<string>>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<SelectListItemDTO<string>>> Handle(GetEventYearsQuery request, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                SELECT DISTINCT YEAR(re.StartDate) AS Id, CAST(YEAR(re.StartDate) AS VARCHAR(4)) AS Text
                FROM ResultEvents AS re
                INNER JOIN ResultCompetition rc  ON re.EventId = rc.EventId AND rc.IsDeleted = 0 
                   AND rc.CompetitionStatusId = 2
                WHERE re.StartDate IS NOT NULL
                ORDER BY Id DESC;";

            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, null, null, commandType: "text")).ToList();
            return result;
        }
    }
}