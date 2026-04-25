using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.Caching
{
    public class RedisConfig
    {
        public string? TenantId { get; set; }
        public string? Host { get; set; }
        public string? AccessKey { get; set; }
        public string? ApplicationName { get; set; }
        public bool RedisCache { get; set; }
    }
}
