using System.Threading;

namespace JustGo.Authentication.Persistence.Repositories.GenericRepositories
{
    public static class DatabaseSwitcher
    {
        private static readonly AsyncLocal<DatabaseType?> _currentDatabaseType = new AsyncLocal<DatabaseType?>();
        
        public static void UseCentralDatabase()
        {
            _currentDatabaseType.Value = DatabaseType.Central;
        }
        public static void UseTenantDatabase()
        {
            _currentDatabaseType.Value = DatabaseType.Tenant;
        }
        public static void UseAzolveCentralDatabase()
        {
            _currentDatabaseType.Value = DatabaseType.AzolveCentral;
        }
        public static void UseAddressPickerCoreDatabase()
        {
            _currentDatabaseType.Value = DatabaseType.AddressPickerCore;
        }
        public static DatabaseType GetCurrentDatabase()
        {
            return _currentDatabaseType.Value ?? DatabaseType.Tenant;
        }


    }
}
