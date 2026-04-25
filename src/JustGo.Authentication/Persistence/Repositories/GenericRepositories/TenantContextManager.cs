using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JustGo.Authentication.Persistence.Repositories.GenericRepositories
{
    public static class TenantContextManager
    {
        private static readonly AsyncLocal<string?> TenantIdContext = new();
        private static readonly AsyncLocal<string?> TenantClientIdContext = new();

        public static void SetTenantId(string tenantId)
        {
            TenantIdContext.Value = tenantId;
        }
        public static string? GetTenantId()
        {
            return TenantIdContext.Value ?? null;
        }
        public static void ClearTenantId()
        {
            TenantIdContext.Value = null;
        }
        public static void SetTenantClientId(string tenantId)
        {
            TenantClientIdContext.Value = tenantId;
        }
        public static string? GetTenantClientId()
        {
            return TenantClientIdContext.Value ?? null;
        }
        public static void ClearTenantClientId()
        {
            TenantClientIdContext.Value = null;
        }

    }    
}
