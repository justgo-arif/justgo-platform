using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JustGo.Authentication.Services.Interfaces.Infrastructure.Caching
{
    public interface IHybridCacheService
    {
        Task<T> GetOrSetAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan duration,
            string[] tags,
            CancellationToken cancellationToken = default
        );
        Task SetAsync<T>(
            string key,
            T value,
            TimeSpan duration,
            string[] tags,
            CancellationToken cancellationToken = default
        );

        Task RemoveAsync(
            string key,
            CancellationToken cancellationToken = default
        );

        Task RemoveByTagAsync(
            string tag,
            CancellationToken cancellationToken = default
        );

        Task<(bool Found, T Value)> TryGetAsync<T>(
            string key,
            CancellationToken cancellationToken = default
        );

        


    }
}
