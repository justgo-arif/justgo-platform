using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories
{
    public interface IMultipleResultReader : IDisposable, IAsyncDisposable
    {
        Task<IEnumerable<T>> ReadAsync<T>();
        Task<T?> ReadSingleOrDefaultAsync<T>();
        Task<T?> ReadSingleAsync<T>();
    }
}
