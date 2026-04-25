using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.CustomAuthorizations;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Authentication.Infrastructure.CustomAuthorizations
{
    public class AuthorizationService: IAuthorizationService
    {
        private readonly IReadRepositoryFactory _readRepository;

        public AuthorizationService(IReadRepositoryFactory readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task IsActionAllowedAsync(int invokingUserId, int docId, string option, CancellationToken cancellationToken)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Result", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
            parameters.Add("@InvokingUserId", invokingUserId);
            parameters.Add("@DocId", docId);
            parameters.Add("@Argument", option);

            await _readRepository.GetLazyRepository<dynamic>().Value.GetListAsync("IsActionAllowed", cancellationToken, parameters);
            var res = parameters.Get<string>("@Result");

            if (!string.IsNullOrEmpty(res))
                throw new UnauthorizedAccessException(res);
        }
    }
}
