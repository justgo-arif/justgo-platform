using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs.FinanceGridViewDtos;
using Newtonsoft.Json;

namespace JustGo.Finance.Application.Features.FinanceGridView.Queries.GetFinanceGridViews
{
    public class GetFinanceGridViewsQueryHandler : IRequestHandler<GetFinanceGridViewsQuery, List<FinanceGridViewDto>?>
    {
        private readonly LazyService<IReadRepository<FinanceGridViewDto>> _readRepository;
        private readonly IUtilityService _utilityService;

        public GetFinanceGridViewsQueryHandler(LazyService<IReadRepository<FinanceGridViewDto>> readRepository, IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _utilityService = utilityService;
        }

        public async Task<List<FinanceGridViewDto>?> Handle(GetFinanceGridViewsQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            var parameters = new DynamicParameters();

            parameters.Add("MerchantId", request.MerchantId);
            parameters.Add("UserId", currentUserId);
            parameters.Add("EntityType", request.EntityType);

            var sql = $@"WITH PINNED AS (
	            SELECT P.ViewId, 1 IsPinned
	            FROM FinanceGridViewPreference P
	            INNER JOIN FinanceGridView V ON V.ViewId = P.ViewId AND V.EntityType = @EntityType
	            WHERE P.IsPinned = 1  
	            AND P.UserId = @UserId AND V.MerchantId = @MerchantId
	            GROUP BY P.ViewId
            ),
            DEFLT AS (
	            SELECT P.ViewId, 1 IsDefault
	            FROM FinanceGridViewPreference P
	            INNER JOIN FinanceGridView V ON V.ViewId = P.ViewId AND V.EntityType = @EntityType
	            WHERE P.IsDefault = 1 --Default
	            AND P.UserId = @UserId AND V.MerchantId = @MerchantId
	            GROUP BY P.ViewId
            )
            SELECT V.ViewId, V.Name, V.IsSystemDefault, ISNULL(P.IsPinned, 0) IsPinned, ISNULL(D.IsDefault, 0) IsDefault
            ,V.Payload,v.MerchantId
            FROM FinanceGridView V
            LEFT JOIN PINNED P ON P.ViewId = V.ViewId
            LEFT JOIN DEFLT D ON D.ViewId = V.ViewId
            WHERE V.EntityType = @EntityType AND V.MerchantId = @MerchantId 
            AND (V.IsSystemDefault = 1 OR  V.CreatedBy = @UserId)
            ORDER BY IsPinned DESC, IsSystemDefault DESC, V.ViewId DESC";

            var results = (await _readRepository.Value.GetListAsync(sql, cancellationToken, parameters, null, "text")).ToList();
            if (results == null || !results.Any())
                return null;

            return results.Select(result => new FinanceGridViewDto
            {
                ViewId = result.ViewId,
                Name = result.Name, 
                MerchantId = result.MerchantId,
                IsSystemDefault = result.IsSystemDefault,
                IsShared = result.IsShared,
                IsPinned = result.IsPinned,
                Payload = string.IsNullOrWhiteSpace(result.Payload)
                            ? new object()
                            : JsonConvert.DeserializeObject<object>(result.Payload)
            }).ToList();


        }
    }
}
