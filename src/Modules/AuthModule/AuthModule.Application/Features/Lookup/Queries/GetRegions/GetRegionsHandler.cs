using AuthModule.Application.DTOs.Lookup;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace AuthModule.Application.Features.Lookup.Queries.GetRegions;

public class GetRegionsHandler : IRequestHandler<GetRegionsQuery, List<SelectListItemDTO<string>>>
{
    private readonly LazyService<IReadRepository<SelectListItemDTO<string>>> _repo;

    public GetRegionsHandler(LazyService<IReadRepository<SelectListItemDTO<string>>> repo)
    {
        _repo = repo;
    }

    public async Task<List<SelectListItemDTO<string>>> Handle(GetRegionsQuery request, CancellationToken cancellationToken)
    {
        var regionSql = @"
            DECLARE @Sql varchar(5000) = (SELECT dbo.GetLookupTableQuery('County'))
            SET @Sql = '
            WITH LU AS (' + @Sql + ')
            SELECT [County] [Value], [County] [Text]
            FROM LU 
            WHERE ISNULL(County, '''') != ''''
            ORDER BY County ASC
            '
            EXEC(@Sql)";

        var regions = (await _repo.Value.GetListAsync(regionSql, cancellationToken, null, null, commandType: "text")).ToList();
        return regions;

    }
}
