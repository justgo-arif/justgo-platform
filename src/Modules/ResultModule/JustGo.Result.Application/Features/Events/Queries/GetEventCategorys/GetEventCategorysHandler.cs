using AuthModule.Application.DTOs.Lookup;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Result.Application.Features.Events.Queries.GetEventCategorys
{

    public class GetEventCategorysHandler : IRequestHandler<GetEventCategorysQuery, List<SelectListItemDTO<string>>>
    {
        private readonly LazyService<IReadRepository<SelectListItemDTO<string>>> _readRepository;
        private readonly IUtilityService _utilityService;

        public GetEventCategorysHandler(LazyService<IReadRepository<SelectListItemDTO<string>>> readRepository, IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _utilityService = utilityService;
        }

        public async Task<List<SelectListItemDTO<string>>> Handle(GetEventCategorysQuery request, CancellationToken cancellationToken = default)
        {
            var resultEventTypeId = await _utilityService.GetEventTypeIdByGuid(request.ResultEventTypeId, cancellationToken);

            var eventTypeCondition = resultEventTypeId.HasValue ? " AND re.ResultEventTypeId = @ResultEventTypeId" : "";

            var sql = $@"
               SELECT DISTINCT
                  ec.EventCategoryId AS Id,
                  ec.CategoryName AS Text
              FROM ResultEvents AS re
              INNER JOIN ResultCompetition rc  ON re.EventId = rc.EventId  AND rc.IsDeleted = 0  AND rc.CompetitionStatusId = 2
              INNER JOIN ResultEventCategory AS ec   ON re.CategoryId = ec.EventCategoryId
              WHERE re.CategoryId IS NOT NULL {eventTypeCondition}";

            var parameters = resultEventTypeId.HasValue
                ? new { ResultEventTypeId = resultEventTypeId.HasValue }
                : null;

            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, parameters, null, commandType: "text")).ToList();
            return result;
        }
    }
}
