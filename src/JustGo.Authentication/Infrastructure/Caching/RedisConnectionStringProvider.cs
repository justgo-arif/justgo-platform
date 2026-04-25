using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using StackExchange.Redis;

namespace JustGo.Authentication.Infrastructure.Caching
{
    public class RedisConnectionStringProvider: IRedisConnectionStringProvider
    {
        private readonly ISystemSettingsService _systemSettingsService;

        public RedisConnectionStringProvider(ISystemSettingsService systemSettingsService)
        {
            _systemSettingsService = systemSettingsService;
        }

        public async Task<string?> GetRedisConnectionStringAsync(CancellationToken cancellationToken)
        {
            var itemKeys = new List<string>()
                {
                    "REDIS.CONFIG", "APPLICATION.NAME", "REDIS.CACHE"
                };
            var systemSettings = await _systemSettingsService.GetSystemSettingsByMultipleItemKey(itemKeys, cancellationToken);
            var redisSettings = systemSettings?.Where(w => w.ItemKey == "REDIS.CONFIG")?.Select(s => s.Value).SingleOrDefault();
            var applicationName = systemSettings?.Where(w => w.ItemKey == "APPLICATION.NAME")?.Select(s => s.Value).SingleOrDefault();
            var redisCache = systemSettings?.Where(w => w.ItemKey == "REDIS.CACHE")?.Select(s => s.Value).SingleOrDefault();
            if (redisSettings == null)
            {
                return null;
            }
            var redisConfig = JsonSerializer.Deserialize<RedisConfig>(redisSettings);
            if (redisConfig == null)
            {
                return null;
            }
            redisConfig.ApplicationName = applicationName;
            redisConfig.RedisCache = redisCache != null ? Convert.ToBoolean(redisCache) : true;
            if (!redisConfig.RedisCache)
            {
                return null;
            }

            var connectionString = $"{redisConfig.Host},password={redisConfig.AccessKey}";
            return connectionString;
        }
    }
}
