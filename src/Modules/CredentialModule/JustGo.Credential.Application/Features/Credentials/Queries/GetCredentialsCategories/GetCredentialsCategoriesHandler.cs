using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Credential.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Credential.Application.Features.Credentials.Queries.GetCredentialsCategories
{
    public class GetCredentialsCategoriesHandler : IRequestHandler<GetCredentialsCategoriesQuery, List<CredentialsCategoriesDto>>
    {
        private readonly LazyService<IReadRepository<CredentialsCategoriesDto>> _readRepository;

        public GetCredentialsCategoriesHandler(LazyService<IReadRepository<CredentialsCategoriesDto>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<List<CredentialsCategoriesDto>> Handle(GetCredentialsCategoriesQuery request, CancellationToken cancellationToken)
        {
            string sql = """        
                       DECLARE @Sql VARCHAR(MAX) = (SELECT dbo.GetLookupTableQuery('CredentialMasterCategory'));

                       SET @Sql = 'WITH LU AS (' + @Sql + ')
                       SELECT RowId AS Id,[Name] AS CredentialCategory FROM LU
                       ORDER BY [Name] ASC';
                         
                       EXEC(@Sql);
                       """;

            var result = (await _readRepository.Value.GetListAsync(sql, cancellationToken, null, null, commandType: "text")).ToList();

            return result;
        }
    }
}   

