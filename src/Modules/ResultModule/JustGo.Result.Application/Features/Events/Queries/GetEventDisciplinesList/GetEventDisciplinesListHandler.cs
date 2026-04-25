using AuthModule.Application.DTOs.Lookup;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.Events.Queries.GetEventDisciplinesList
{
    public class GetEventDisciplinesListHandler : IRequestHandler<GetEventDisciplinesListQuery, List<SelectListItemDTO<string>>>
    {
        private readonly LazyService<IReadRepository<SelectListItemDTO<string>>> _readRepository;

        public GetEventDisciplinesListHandler(LazyService<IReadRepository<SelectListItemDTO<string>>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<SelectListItemDTO<string>>> Handle(GetEventDisciplinesListQuery request, CancellationToken cancellationToken = default)
        {
            const string sql = @"
                 SELECT DISTINCT
                    de.DisciplineId AS Id,
                    de.Name AS Text
                FROM ResultDisciplines AS de
                INNER JOIN ResultCompetition rc ON de.DisciplineId = rc.DisciplineId AND rc.IsDeleted = 0 AND rc.CompetitionStatusId = 2
                ORDER BY de.Name";

            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, null, null, commandType: "text")).ToList();
            return result;
        }
    }
}