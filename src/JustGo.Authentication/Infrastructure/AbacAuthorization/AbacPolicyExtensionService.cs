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
    public class AbacPolicyExtensionService: IAbacPolicyExtensionService
    {
        private readonly LazyService<IReadRepository<AbacPolicyExtension>> _readRepository;
        private readonly IHybridCacheService _cache;
        public AbacPolicyExtensionService(LazyService<IReadRepository<AbacPolicyExtension>> readRepository, IHybridCacheService cache)
        {
            _readRepository = readRepository;
            _cache = cache;
        }

        public async Task<List<AbacPolicyExtension>> GetPolicyExtensionByPolicyName(string policyName, CancellationToken cancellationToken)
        {
            string sql = @"SELECT pe.[PolicyExtensionId]
                              ,pe.[ResourceKey]
                              ,pe.[ReturnType]
                              ,pe.[SqlQuery]
                              ,pe.[SqlParams]
                        FROM [dbo].[AbacPolicyExtensions] pe
							INNER JOIN [dbo].[AbacPolicyExtensionMapping] pem 
								ON pe.PolicyExtensionId=pem.[PolicyExtensionId]
	                        INNER JOIN [dbo].[AbacPolicies] p ON p.Id=pem.PolicyId
                        WHERE p.PolicyName=@PolicyName";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@PolicyName", policyName);
            var cacheKey = $"policy_extensions_{policyName}";
            return await _cache.GetOrSetAsync<List<AbacPolicyExtension>>(
                cacheKey,
                async _ => (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList(),
                TimeSpan.FromMinutes(30),
                new[] { CacheTag.ABAC.ToString() },
                cancellationToken
                );
        }
    }
}
