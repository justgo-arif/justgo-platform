using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Authentication.Persistence.Repositories.GenericRepositories
{
    public class MultipleResultReader : IMultipleResultReader
    {
        private readonly SqlMapper.GridReader _gridReader;
        private readonly IDbConnection? _connection;
        public MultipleResultReader(SqlMapper.GridReader gridReader, IDbConnection? connection = null)
        {
            _gridReader = gridReader;
            _connection = connection;
        }

        public async Task<IEnumerable<T>> ReadAsync<T>() =>
            await _gridReader.ReadAsync<T>();

#if NET9_0_OR_GREATER
        public async Task<T?> ReadSingleOrDefaultAsync<T>() =>
            await _gridReader.ReadSingleOrDefaultAsync<T>();
#else
        public async Task<T?> ReadSingleOrDefaultAsync<T>() =>
                    (await _gridReader.ReadAsync<T>()).SingleOrDefault();
#endif
#if NET9_0_OR_GREATER
        public async Task<T?> ReadSingleAsync<T>() =>
            await _gridReader.ReadSingleAsync<T>();
#else
        public async Task<T?> ReadSingleAsync<T>() =>
                    (await _gridReader.ReadAsync<T>()).Single();
#endif

        public async ValueTask DisposeAsync()
        {
            if (_gridReader is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else
                _gridReader.Dispose();

            if (_connection is IAsyncDisposable asyncDisposableConn)
                await asyncDisposableConn.DisposeAsync();
            else
                _connection?.Dispose();
        }

        public void Dispose()
        {
            _gridReader.Dispose();
            _connection?.Dispose();
        }


    }
}
