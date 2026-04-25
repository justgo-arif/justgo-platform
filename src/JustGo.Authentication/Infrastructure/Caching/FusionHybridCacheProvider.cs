using System.Collections.Concurrent;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;

#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace JustGo.Authentication.Infrastructure.Caching
{
    public class FusionHybridCacheProvider : IFusionHybridCacheProvider
    {
        private readonly IRedisConnectionStringProvider _redisConnectionStringProvider;
        private readonly IMemoryCache _memoryCache;
        private readonly IUtilityService _utilityService;
        private readonly ConcurrentDictionary<string, Task<IFusionCache>> _caches = new();

        public FusionHybridCacheProvider(IRedisConnectionStringProvider redisConnectionStringProvider, IMemoryCache memoryCache
            , IUtilityService utilityService)
        {
            _redisConnectionStringProvider = redisConnectionStringProvider;
            _memoryCache = memoryCache;
            _utilityService = utilityService;
        }
        public async Task<IFusionCache> GetCacheAsync(string? tenantId, CancellationToken cancellationToken)
        {
            tenantId ??= await _utilityService.GetCurrentTenantGuid(cancellationToken);
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new InvalidOperationException("Tenant ID is missing from the current context.");
            return await _caches.GetOrAdd(tenantId, id => CreateCacheAsync(tenantId, cancellationToken));
        }

        private async Task<IFusionCache> CreateCacheAsync(string tenantId, CancellationToken cancellationToken)
        {
            var cacheOptions = new FusionCacheOptions
            {
                DefaultEntryOptions = new FusionCacheEntryOptions
                {
                    Duration = TimeSpan.FromMinutes(15), // Longer duration
                    IsFailSafeEnabled = true,
                    FailSafeMaxDuration = TimeSpan.FromHours(2), // Much longer fail-safe
                    FailSafeThrottleDuration = TimeSpan.FromMinutes(1), // Slower retries
                    Priority = CacheItemPriority.Low,

                    // Lazy mode settings
                    EagerRefreshThreshold = 0.95f, // Very late refresh
                    JitterMaxDuration = TimeSpan.FromSeconds(10) // More jitter
                },

            };

            var cache = new FusionCache(cacheOptions, _memoryCache);

            var redisConnection = await _redisConnectionStringProvider.GetRedisConnectionStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(redisConnection))
            {
                var redis = new RedisCache(new RedisCacheOptions()
                {
                    Configuration = redisConnection,
                    ConnectionMultiplexerFactory = () =>
                    {
                        var configOptions = ConfigurationOptions.Parse(redisConnection);
                        configOptions.ConnectTimeout = 60000;      // 60 seconds
                        configOptions.SyncTimeout = 60000;         // 60 seconds
                        configOptions.AsyncTimeout = 60000;        // 60 seconds
                        configOptions.Ssl = true;
                        configOptions.AbortOnConnectFail = false;

                        configOptions.ConnectRetry = 5; // Increase retries for stability
                        configOptions.ReconnectRetryPolicy = new ExponentialRetry(10000); // Increase retry wait time
                        configOptions.ResolveDns = true;
                        configOptions.KeepAlive = 180; // Prevents idle disconnections for a longer period
                        configOptions.DefaultDatabase = 0;
                        configOptions.AllowAdmin = true; // Allows advanced Redis commands
                        configOptions.ClientName = tenantId;
                        return ConnectionMultiplexer.ConnectAsync(configOptions)
                        .ContinueWith<Task<IConnectionMultiplexer>>(t => Task.FromResult((IConnectionMultiplexer)t.Result)).Unwrap();
                    }
                });

                var serializer = new FusionCacheSystemTextJsonSerializer();
                cache.SetupDistributedCache(redis, serializer);

                var redisOptions = new RedisBackplaneOptions()
                {
                    Configuration = redisConnection
                };
                var backplane = new RedisBackplane(redisOptions);
                cache.SetupBackplane(backplane);
            }

            return cache;
        }


    }
}
#endif
