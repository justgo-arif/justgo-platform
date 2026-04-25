using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AuthModule.Application.Features.Tenants.Queries.GetTenants;
using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace AuthModule.Application.Features.Policies.Queries.GetPolicies
{
    public class GetPoliciesHandler : IRequestHandler<GetPoliciesQuery, List<Policy>>
    {
        private readonly LazyService<IReadRepository<Policy>> _readRepository;

        public GetPoliciesHandler(LazyService<IReadRepository<Policy>> readRepository)
        {
            _readRepository = readRepository;
        }
        public async Task<List<Policy>> Handle(GetPoliciesQuery request, CancellationToken cancellationToken)
        {
            string sql = $@"SELECT [Id]
                                  ,[PolicyName]
                                  ,[PolicyDescription]
                                  ,[PolicyRule]
                                  ,[ParentPolicyId]
                              FROM [dbo].[Policies]";
            var queryParameters = new DynamicParameters();
            var policies = await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text");
            return policies.AsList();
        }
    }
}
