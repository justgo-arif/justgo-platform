using AuthModule.Application.DTOs.Lookup;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Lookup.Queries.GetCountrys
{
    public class GetCountrysHandler : IRequestHandler<GetCountrysQuery, List<SelectListItemDTO<string>>>
    {
        private readonly LazyService<IReadRepository<SelectListItemDTO<string>>> _readRepository;

        public GetCountrysHandler(LazyService<IReadRepository<SelectListItemDTO<string>>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<SelectListItemDTO<string>>> Handle(GetCountrysQuery request, CancellationToken cancellationToken)
        {
            const string sql = @"
            DECLARE @Sql VARCHAR(MAX) = (SELECT dbo.GetLookupTableQuery('Country'));

            SET @Sql = '
                WITH LU AS (' + @Sql + ')
                SELECT RowId AS Id, Country AS Text, CountryCode AS Value
                FROM LU
                ORDER BY Country ASC';

            EXEC(@Sql);";

            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, null, null, commandType: "text")).ToList();
            return result;
        }

    }
}

