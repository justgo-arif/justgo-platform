using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;

namespace JustGo.Authentication.Services.Interfaces.Infrastructure.Caching
{
    public interface IFusionHybridCacheProvider
    {
        Task<IFusionCache> GetCacheAsync(string? tenantId, CancellationToken cancellationToken);
    }
}
