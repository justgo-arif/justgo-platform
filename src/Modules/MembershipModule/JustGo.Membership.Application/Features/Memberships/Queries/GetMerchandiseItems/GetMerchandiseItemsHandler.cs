using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Membership.Application.DTOs;
using Microsoft.Extensions.Caching.Memory;
using System.Data;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetMerchandiseItems
{
    public class GetMerchandiseItemsHandler : IRequestHandler<GetMerchandiseItemsQuery, List<MerchandiseItemsDto>>
    {
        private readonly LazyService<IReadRepository<MerchandiseItemsDto>> _readRepository;
        private readonly ISystemSettingsService _systemSettingsService;
        private readonly IMemoryCache _cache;

        public GetMerchandiseItemsHandler(
            LazyService<IReadRepository<MerchandiseItemsDto>> readRepository,
            ISystemSettingsService systemSettingsService,
            IMemoryCache cache)
        {
            _readRepository = readRepository;
            _systemSettingsService = systemSettingsService;
            _cache = cache;
        }

        public async Task<List<MerchandiseItemsDto>> Handle(GetMerchandiseItemsQuery request, CancellationToken cancellationToken)
        {
            var ids = request?.Ids ?? new List<string>();
            var cacheKey = $"MerchandiseItems:{string.Join(",", ids.OrderBy(x => x))}";

            if (_cache.TryGetValue(cacheKey, out List<MerchandiseItemsDto> cachedResult))
            {
                return cachedResult;
            }

            var userSyncGuids = ids.Where(x => x != "0").ToList();
            bool includeNgb = ids.Contains("0");

            var ngbName = string.Empty;

            var sql = @"
        SELECT
            p.Category,
            p.Name AS ProductName,
            c.ClubName,
            p.UnitPrice,
            p.Location AS ProductImage
        FROM Products_Default p
        INNER JOIN Clubs_Default c ON p.OwnerId = c.DocId
        INNER JOIN Document doc ON doc.DocId = c.DocId
        WHERE p.Category = 'Upsale' AND doc.SyncGuid IN @UserSyncGuids
    ";

            if (includeNgb)
            {
                ngbName = _systemSettingsService != null
                    ? await _systemSettingsService.GetSystemSettingsByItemKey("ORGANISATION.NAME", cancellationToken)
                    : null;

                if (string.IsNullOrEmpty(ngbName))
                    ngbName = "Organisation";

                sql += @"
            UNION ALL
            SELECT
                p.Category,
                p.Name AS ProductName,
                @NgbName AS ClubName,
                p.UnitPrice,
                p.Location AS ProductImage
            FROM Products_Default p
            WHERE p.Category = 'Upsale' AND p.OwnerId = '0'
        ";
            }

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@NgbName", ngbName);
            queryParameters.Add("@UserSyncGuids", userSyncGuids);

            var result = new List<MerchandiseItemsDto>();

            if (_readRepository?.Value != null)
            {
                var data = await _readRepository.Value.GetListAsync(
                    sql,
                    cancellationToken,
                    queryParameters,
                    null,
                    "text"
                );

                result = data?.ToList() ?? new List<MerchandiseItemsDto>();
            }

            // Cache the result for 5 minutes
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

            return result;
        }

    }
}
