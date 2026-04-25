using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Policies.Queries.GetPolicyByName
{
    public class GetPolicyByNameHandler: IRequestHandler<GetPolicyByNameQuery,Policy>
    {
        private readonly LazyService<IReadRepository<Policy>> _readRepository;

        public GetPolicyByNameHandler(LazyService<IReadRepository<Policy>> readRepository)
        {
            _readRepository = readRepository;
        }

        public async Task<Policy> Handle(GetPolicyByNameQuery request, CancellationToken cancellationToken)
        {
            string sql = @"SELECT [Id]
                              ,[PolicyName]
                              ,[PolicyDescription]
                              ,[PolicyRule]
                              ,[ParentPolicyId]
                          FROM [dbo].[Policies]
                          WHERE [PolicyName]=@PolicyName";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@PolicyName", request.PolicyName);
            return await _readRepository.Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
        }
    }
}
