using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Http;
namespace JustGo.Authentication.Infrastructure.Caching
{
    public class HybridCacheService: IHybridCacheService
    {
        private readonly IFusionHybridCacheProvider _cacheProvider;
        private readonly IUtilityService _utilityService;
        public HybridCacheService(IFusionHybridCacheProvider cacheProvider
            , IUtilityService utilityService)
        {
            _cacheProvider = cacheProvider;
            _utilityService = utilityService;
        }

        public async Task<T> GetOrSetAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan duration,
            string[] tags,
            CancellationToken cancellationToken = default)
        {
            var tenantId = await _utilityService.GetCurrentTenantGuid(cancellationToken);
            var cache = await _cacheProvider.GetCacheAsync(tenantId, cancellationToken);
            var prefixedKey = await GetPrefixedKey(key, tenantId, cancellationToken);
            var prefixedTags = await GetPrefixedTags(tags, tenantId, cancellationToken);

            return await cache.GetOrSetAsync<T>(
                prefixedKey,
                async (ctx, token) => await factory(token),
                options: cache.CreateEntryOptions(duration: duration),
                tags: prefixedTags,
                token: cancellationToken
            );
        }

        public async Task SetAsync<T>(
            string key,
            T value,
            TimeSpan duration,
            string[] tags,
            CancellationToken cancellationToken = default)
        {
            var tenantId = await _utilityService.GetCurrentTenantGuid(cancellationToken);
            var cache = await _cacheProvider.GetCacheAsync(tenantId, cancellationToken);
            var prefixedKey = await GetPrefixedKey(key, tenantId, cancellationToken);
            var prefixedTags = await GetPrefixedTags(tags, tenantId, cancellationToken);

            await cache.SetAsync<T>(
                prefixedKey,
                value,
                options: cache.CreateEntryOptions(duration: duration),
                tags: prefixedTags,
                token: cancellationToken
            );
        }

        public async Task RemoveAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            var tenantId = await _utilityService.GetCurrentTenantGuid(cancellationToken);
            var cache = await _cacheProvider.GetCacheAsync(tenantId, cancellationToken);
            var prefixedKey = await GetPrefixedKey(key, tenantId, cancellationToken);
            await cache.RemoveAsync(prefixedKey, token: cancellationToken);
        }

        public async Task RemoveByTagAsync(
            string tag,
            CancellationToken cancellationToken = default)
        {
            var tenantId = await _utilityService.GetCurrentTenantGuid(cancellationToken);
            var cache = await _cacheProvider.GetCacheAsync(tenantId, cancellationToken);
            var prefixedTag = await GetPrefixedTag(tag, tenantId, cancellationToken);
            await cache.RemoveByTagAsync(prefixedTag, token: cancellationToken);
        }

        public async Task<(bool Found, T Value)> TryGetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var tenantId = await _utilityService.GetCurrentTenantGuid(cancellationToken);
            var cache = await _cacheProvider.GetCacheAsync(tenantId, cancellationToken);
            var prefixedKey = await GetPrefixedKey(key, tenantId, cancellationToken);

            var result = await cache.TryGetAsync<T>(
                prefixedKey,
                token: cancellationToken
            );

            return result.HasValue ? (true, result.Value) : (false, default!);
        }

        private async Task<string> GetPrefixedKey(string key, string? tenantId, CancellationToken cancellationToken = default)
        {
            tenantId ??= await _utilityService.GetCurrentTenantGuid(cancellationToken);
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new InvalidOperationException("Tenant ID is missing from the current context.");
            return $"{tenantId}_{key}";
        }
        private async Task<string[]> GetPrefixedTags(string[] tags, string? tenantId, CancellationToken cancellationToken = default)
        {
            tenantId ??= await _utilityService.GetCurrentTenantGuid(cancellationToken);
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new InvalidOperationException("Tenant ID is missing from the current context.");
            return tags.Select(tag => $"{tenantId}_{tag}").ToArray();
        }

        private async Task<string> GetPrefixedTag(string tag, string? tenantId, CancellationToken cancellationToken = default)
        {
            tenantId ??= await _utilityService.GetCurrentTenantGuid(cancellationToken);
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new InvalidOperationException("Tenant ID is missing from the current context.");
            return $"{tenantId}_{tag}";
        }
    }
}
#endif