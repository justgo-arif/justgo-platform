using AuthModule.Application.DTOs.Lookup;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Lookup.Queries.GetCountys
{
    public class GetCountysHandler : IRequestHandler<GetCountysQuery, List<SelectListItemDTO<string>>>
    {
        private readonly LazyService<IReadRepository<SelectListItemDTO<string>>> _readRepository;

        public GetCountysHandler(LazyService<IReadRepository<SelectListItemDTO<string>>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<SelectListItemDTO<string>>> Handle(GetCountysQuery request, CancellationToken cancellationToken)
        {

            const string sql = @"
        DECLARE @Sql VARCHAR(MAX) = (SELECT dbo.GetLookupTableQuery('County'));

        SET @Sql = '
            WITH LU AS (' + @Sql + ')
            SELECT RowId AS Id, County AS Text
            FROM LU
            --ORDER BY County ASC
            ';

        EXEC(@Sql);";

            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, null, null, commandType: "text")).ToList();
            return result;
        }
    }
}

