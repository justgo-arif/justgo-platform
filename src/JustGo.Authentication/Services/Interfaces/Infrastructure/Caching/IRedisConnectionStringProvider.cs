using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JustGo.Authentication.Services.Interfaces.Infrastructure.Caching
{
    public interface IRedisConnectionStringProvider
    {
        Task<string?> GetRedisConnectionStringAsync(CancellationToken cancellationToken);
    }
}
