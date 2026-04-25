#if NET9_0_OR_GREATER
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using Dapper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;

namespace JustGo.Authentication.Persistence.Repositories.GenericRepositories
{
    public partial class DatabaseProvider
    {
        private readonly IConfiguration _config;
        public DatabaseProvider(IConfiguration config
            , IUtilityService utilityService)
        {
            _config = config;
            _utilityService = utilityService;
            _centralDbConnection = _config.GetConnectionString("ApiConnection") ?? throw new InvalidOperationException("ApiConnection string is missing.");
            _azolveCentralDbConnection = _config.GetConnectionString("AzolveCentralDB") ?? throw new InvalidOperationException("AzolveCentralDB connection string is missing.");
            _addressPickerCoreDbConnection = _config.GetConnectionString("AddressPickerCore") ?? throw new InvalidOperationException("AddressPickerCore connection string is missing.");
        }

        private async Task<string> GetTenantConnectionStringAsync(bool isRead)
        {            
            var tenantClientId = _utilityService.GetCurrentTenantClientId();
            if (tenantClientId is null)
            {
                throw new Exception("Tenant ID is missing in request");
            }            
            string sql = @"SELECT td.* FROM [dbo].[Tenants] t
	                            INNER JOIN [dbo].[TenantDatabases] td ON t.Id=td.TenantId
                            WHERE t.[TenantClientId]=@TenantClientId AND td.[IsReadDatabase]=@IsReadDatabase";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@TenantClientId", tenantClientId);
            queryParameters.Add("@IsReadDatabase", isRead);
            if (isRead)
            {
                return await GetReadTenantConnectionStringAsync(tenantClientId, sql, queryParameters);
            }
            else
            {
                return await GetWriteTenantConnectionStringAsync(tenantClientId, sql, queryParameters);
            }
        }
        private async Task<string> GetReadTenantConnectionStringAsync(string tenantClientId, string sql, DynamicParameters queryParameters)
        {
            if (!_readTenantConnections.TryGetValue(tenantClientId, out var cachedReadConnections))
            {
                var connectionList = new List<string>();
                using var connection = new SqlConnection(_centralDbConnection);
                var tenants = (await connection.QueryAsync<dynamic>(sql, queryParameters)).ToList();
                if (!tenants.Any())
                {
                    throw new Exception("No read databases found for the tenant.");
                }
                foreach (var tenant in tenants)
                {
                    connectionList.Add(BuildConnectionString(tenant));
                }
                _readTenantConnections[tenantClientId] = connectionList;
                cachedReadConnections = connectionList;
            }

            var tenantConnectionList = cachedReadConnections as IList<string>;
            if (tenantConnectionList is null)
            {
                throw new Exception("No cached read databases found for the tenant.");
            }
            lock (_lock)
            {
                var currentIndex = _tenantCurrentIndices.AddOrUpdate(
                    tenantClientId,
                    0, // Initial value for new tenant
                    (key, oldValue) => (oldValue + 1) % tenantConnectionList.Count // Increment and wrap around
                );
                var connectionSring = tenantConnectionList[currentIndex];
                return connectionSring;
            }
        }
        private async Task<string> GetWriteTenantConnectionStringAsync(string tenantClientId, string sql, DynamicParameters queryParameters)
        {
            if (!_writeTenantConnections.TryGetValue(tenantClientId, out var cachedWriteConnection))
            {
                using var connection = new SqlConnection(_centralDbConnection);
                var tenant = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, queryParameters);
                if (tenant is null)
                {
                    throw new Exception("No write database found for the tenant.");
                }
                var tenantConnectionString = BuildConnectionString(tenant);
                _writeTenantConnections[tenantClientId] = tenantConnectionString;
                cachedWriteConnection = tenantConnectionString;
            }
            if (cachedWriteConnection is null)
            {
                throw new Exception("No cached write database found for the tenant.");
            }
            return cachedWriteConnection;
        }
        private string GetTenantConnectionString(bool isRead)
        {
            var tenantClientId = _utilityService.GetCurrentTenantClientId();
            if (tenantClientId is null)
            {
                throw new Exception("Tenant ID is missing in request");
            }
            string sql = @"SELECT td.* FROM [dbo].[Tenants] t
	                            INNER JOIN [dbo].[TenantDatabases] td ON t.Id=td.TenantId
                            WHERE t.[TenantClientId]=@TenantClientId AND td.[IsReadDatabase]=@IsReadDatabase";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@TenantClientId", tenantClientId);
            queryParameters.Add("@IsReadDatabase", isRead);
            if (isRead)
            {
                return GetReadTenantConnectionString(tenantClientId, sql, queryParameters);
            }
            else
            {
                return GetWriteTenantConnectionString(tenantClientId, sql, queryParameters);
            }
        }
        private string GetReadTenantConnectionString(string tenantClientId, string sql, DynamicParameters queryParameters)
        {
            if (!_readTenantConnections.TryGetValue(tenantClientId, out var cachedReadConnections))
            {
                var connectionList = new List<string>();
                using var connection = new SqlConnection(_centralDbConnection);
                var tenants = (connection.Query<dynamic>(sql, queryParameters)).ToList();
                if (!tenants.Any())
                {
                    throw new Exception("No read databases found for the tenant.");
                }
                foreach (var tenant in tenants)
                {
                    connectionList.Add(BuildConnectionString(tenant));
                }
                _readTenantConnections[tenantClientId] = connectionList;
                cachedReadConnections = connectionList;
            }

            var tenantConnectionList = cachedReadConnections as IList<string>;
            if (tenantConnectionList is null)
            {
                throw new Exception("No cached read databases found for the tenant.");
            }
            lock (_lock)
            {
                var currentIndex = _tenantCurrentIndices.AddOrUpdate(
                    tenantClientId,
                    0, // Initial value for new tenant
                    (key, oldValue) => (oldValue + 1) % tenantConnectionList.Count // Increment and wrap around
                );
                var connectionSring = tenantConnectionList[currentIndex];
                return connectionSring;
            }
        }
        private string GetWriteTenantConnectionString(string tenantClientId, string sql, DynamicParameters queryParameters)
        {
            if (!_writeTenantConnections.TryGetValue(tenantClientId, out var cachedWriteConnection))
            {
                using var connection = new SqlConnection(_centralDbConnection);
                var tenant = connection.QueryFirstOrDefault<dynamic>(sql, queryParameters);
                if (tenant is null)
                {
                    throw new Exception("No write database found for the tenant.");
                }
                var tenantConnectionString = BuildConnectionString(tenant);
                _writeTenantConnections[tenantClientId] = tenantConnectionString;
                cachedWriteConnection = tenantConnectionString;
            }
            if (cachedWriteConnection is null)
            {
                throw new Exception("No cached write database found for the tenant.");
            }
            return cachedWriteConnection;
        }


    }
}
#endif