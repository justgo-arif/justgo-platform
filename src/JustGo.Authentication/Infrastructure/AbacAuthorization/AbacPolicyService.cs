using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Authentication.Infrastructure.AbacAuthorization
{
    public class AbacPolicyService: IAbacPolicyService
    {
        private readonly LazyService<IReadRepository<AbacPolicy>> _readRepository;
        private readonly IHybridCacheService _cache;
        public AbacPolicyService(LazyService<IReadRepository<AbacPolicy>> readRepository, IHybridCacheService cache)
        {
            _readRepository = readRepository;
            _cache = cache;
        }

        public async Task<AbacPolicy?> GetPolicyByName(string policyName, CancellationToken cancellationToken)
        {
            string sql = @"SELECT [Id]
                              ,[PolicyName]
                              ,[PolicyDescription]
                              ,[PolicyRule]
                              ,[PolicyOwner]
                              ,[PolicyEntryPoint]
                          FROM [dbo].[AbacPolicies]
                          WHERE [PolicyName]=@PolicyName";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@PolicyName", policyName);
            //return await _readRepository.Value.GetAsync(sql,cancellationToken, queryParameters, null, "text");
            var cacheKey = $"policy_{policyName}";
            return await _cache.GetOrSetAsync<AbacPolicy?>(
                cacheKey,
                async _ => await _readRepository.Value.GetAsync(sql, cancellationToken, queryParameters, null, "text"),
                TimeSpan.FromMinutes(30),
                new[] { CacheTag.ABAC.ToString() },
                cancellationToken
                );
        }
    }
}
