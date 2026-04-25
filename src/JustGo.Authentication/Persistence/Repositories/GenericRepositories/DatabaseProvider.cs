using System;
using System.Data;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Helper.Enums;
using System.Collections.Concurrent;
#if NET9_0_OR_GREATER
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;

namespace JustGo.Authentication.Persistence.Repositories.GenericRepositories
{
    public partial class DatabaseProvider: IDatabaseProvider
    {
        private readonly string _centralDbConnection;
        private readonly string _azolveCentralDbConnection;
        private readonly string _addressPickerCoreDbConnection;
        private static readonly ConcurrentDictionary<string, object> _readTenantConnections = new ConcurrentDictionary<string, object>();
        private static readonly ConcurrentDictionary<string, string> _writeTenantConnections = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, int> _tenantCurrentIndices = new ConcurrentDictionary<string, int>();
        private readonly object _lock = new object();
        private readonly IUtilityService _utilityService;

        
        public async Task<IDbConnection> GetDbConnectionAsync(bool isRead)
        {
            string connectionString;
            var dbType = DatabaseSwitcher.GetCurrentDatabase();
            switch (dbType)
            {
                case DatabaseType.Central:
                    connectionString = _centralDbConnection;
                    break;
                case DatabaseType.Tenant:
                    connectionString = await GetTenantConnectionStringAsync(isRead);
                    break;
                case DatabaseType.AzolveCentral:
                    connectionString = _azolveCentralDbConnection;
                    break;
                case DatabaseType.AddressPickerCore:
                    connectionString = _addressPickerCoreDbConnection;
                    break;

                default:
                    throw new InvalidOperationException("Unknown database type.");
            }
            DatabaseSwitcher.UseTenantDatabase();
            return new SqlConnection(connectionString);
        }

        public IDbConnection GetDbConnection(bool isRead)
        {
            string connectionString;
            var dbType = DatabaseSwitcher.GetCurrentDatabase();
            switch (dbType)
            {
                case DatabaseType.Central:
                    connectionString = _centralDbConnection;
                    break;
                case DatabaseType.Tenant:
                    connectionString = GetTenantConnectionString(isRead);
                    break;
                case DatabaseType.AzolveCentral:
                    connectionString = _azolveCentralDbConnection;
                    break;
                case DatabaseType.AddressPickerCore:
                    connectionString = _addressPickerCoreDbConnection;
                    break;

                default:
                    throw new InvalidOperationException("Unknown database type.");
            }
            DatabaseSwitcher.UseTenantDatabase();
            return new SqlConnection(connectionString);
        }
        private string BuildConnectionString(dynamic tenant)
        {
            return $"Data Source={tenant.ServerName};Initial Catalog={tenant.DatabaseName};Persist Security Info=False;" +
                   $"Integrated Security=False;User ID={_utilityService.DecryptData(tenant.DBUserId)};Password={_utilityService.DecryptData(tenant.DBPassword)};" +
                   $"TrustServerCertificate=True;Connection Timeout=30;";
        }

    }
}
