using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.Caching
{
    public class CacheInvalidationRequest
    {
        public List<string> Keys { get; set; } = new();
        public List<string> Tags { get; set; } = new();
    }
}
