using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthModule.Domain.Entities
{
    public class Tenant
    {
        public int Id { get; set; }
        public Guid TenantGuid { get; set; }
        public string TenantName { get; set; }
        public string TenantDescription { get; set; }
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string DBUserId { get; set; }
        public string DBPassword { get; set; }
        public string ServerLocation { get; set; }
        public string TenantClientId { get; set; }
        public string ApiUrl { get; set; }
        public string TenantDomainUrl { get; set; }
        public string JwtAccessTokenSecretKey { get; set; }
        public int JwtAccessTokenExpiryMinutes { get; set; }
        public int JwtRefreshTokenExpiryMinutes { get; set; }
        public bool IsReadDatabase { get; set; }
    }
}
