using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using System.Threading;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Azure.Core;
using Dapper;
using System.Collections;

namespace JustGo.Authentication.Infrastructure.SystemSettings
{
    public class SystemSettingsService : ISystemSettingsService
    {
        private readonly LazyService<IReadRepository<SystemSettings>> _readRepository;
        private readonly IUtilityService _utilityService;

        public SystemSettingsService(LazyService<IReadRepository<SystemSettings>> readRepository, IUtilityService utilityService)
        {
            _readRepository = readRepository;
            _utilityService = utilityService;
        }

        public async Task<string?> GetSystemSettingsByItemKey(string itemKey, CancellationToken cancellationToken)
        {
            var sql = @"SELECT [Value]
                              ,ISNULL([Restricted],0) AS Restricted
                        FROM [dbo].[SystemSettings]
                        WHERE [ItemKey] = @ItemKey";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ItemKey", itemKey);
            var result = await _readRepository.Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");

            if (!string.IsNullOrEmpty(result?.Value) && result.Restricted)
            {
                result.Value = _utilityService.DecryptData(result.Value);
            }
            return result?.Value;
        }
        public async Task<string> GetSystemSettings(string key, CancellationToken cancellationToken, string entity = "", int entityId = -1)
        {
            var sql = @"SELECT [ItemId]
                              ,[ItemKey]
                              ,[KeyGroup]
                              ,[ModuleId]
                              ,[Value]
                              ,[IsPersonalizable]
                              ,[IsSyncAble]
                              ,ISNULL([Restricted],0) AS Restricted
                        FROM [dbo].[SystemSettings]
                        WHERE [ItemKey] IN (SELECT VALUE FROM STRING_SPLIT(@Key,','))";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@Key", key);
            var result = await _readRepository.Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");

            if (!string.IsNullOrEmpty(result.Value) && result.Restricted)
            {
                result.Value = _utilityService.DecryptData(result.Value);
            }
            return result.Value;
        }
        public async Task<List<SystemSettings>> GetSystemSettingsByMultipleItemKey(string itemKeys, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(itemKeys))
            {
                return new List<SystemSettings>();
            }
            var query = @"SELECT [ItemId]
                              ,[ItemKey]
                              ,[KeyGroup]
                              ,[ModuleId]
                              ,[Value]
                              ,[IsPersonalizable]
                              ,[IsSyncAble]
                              ,ISNULL([Restricted],0) AS Restricted
                        FROM [dbo].[SystemSettings]
                        WHERE [ItemKey] IN (SELECT VALUE FROM STRING_SPLIT(@ItemKeys,','))";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ItemKeys", itemKeys);
            var results = (await _readRepository.Value.GetListAsync(query, cancellationToken, queryParameters, null, "text")).ToList();
            foreach (var item in results)
            {
                if (!string.IsNullOrEmpty(item.Value) && item.Restricted)
                {
                    item.Value = _utilityService.DecryptData(item.Value);
                }
            }
            return results;
        }
        public async Task<List<SystemSettings>> GetSystemSettingsByMultipleItemKey(List<string> itemKeys, CancellationToken cancellationToken)
        {
            if (itemKeys is null)
            {
                return new List<SystemSettings>();
            }
            var query = @"SELECT [ItemId]
                              ,[ItemKey]
                              ,[KeyGroup]
                              ,[ModuleId]
                              ,[Value]
                              ,[IsPersonalizable]
                              ,[IsSyncAble]
                              ,ISNULL([Restricted],0) AS Restricted
                        FROM [dbo].[SystemSettings]
                        WHERE [ItemKey] IN @ItemKeys";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ItemKeys", itemKeys);
            var results = (await _readRepository.Value.GetListAsync(query, cancellationToken, queryParameters, null, "text")).ToList();
            foreach (var item in results)
            {
                if (!string.IsNullOrEmpty(item.Value) && item.Restricted)
                {
                    item.Value = _utilityService.DecryptData(item.Value);
                }
            }
            return results;
        }
        public async Task<List<SystemSettings>> GetSystemSettingsByMultipleItemKey(string[] itemKeys, CancellationToken cancellationToken)
        {
            if (itemKeys == null || itemKeys.Length == 0)
            {
                return new List<SystemSettings>();
            }
            var query = BuildDynamicQuery(itemKeys);
            var queryParameters = new DynamicParameters();
            for (int i = 0; i < itemKeys.Length; i++)
            {
                queryParameters.Add($"@ItemKey{i}", itemKeys[i]);
            }
            await using var multi = await _readRepository.Value.GetMultipleQueryAsync(query, cancellationToken, queryParameters, null, "text");

            var results = new List<SystemSettings>();
            foreach (var itemKey in itemKeys)
            {
                var result = (await multi.ReadAsync<SystemSettings>()).SingleOrDefault();
                results.Add(result);
            }
            foreach (var item in results)
            {
                if (!string.IsNullOrEmpty(item.Value) && item.Restricted)
                {
                    item.Value = _utilityService.DecryptData(item.Value);
                }
            }
            return results;
        }
        private string BuildDynamicQuery(string[] itemKeys)
        {
            var queries = itemKeys.Select((key, index) => $"SELECT * FROM [dbo].[SystemSettings] WHERE [ItemKey] = @itemKey{index}");
            return string.Join(";\n", queries);
        }

        


    }
}
