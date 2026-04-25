using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Authentication.Persistence.Repositories.GenericRepositories
{
    public class WriteRepository<T> : IWriteRepository<T> where T : class
    {
        private readonly IDatabaseProvider _databaseProvider;
        public WriteRepository(IDatabaseProvider databaseProvider)
        {
            _databaseProvider = databaseProvider;
        }

        private async Task<IDbConnection> GetDbConnectionAsync()
        {
            return await _databaseProvider.GetDbConnectionAsync(false);
        }
        private IDbConnection GetDbConnection()
        {
            return _databaseProvider.GetDbConnection(false);
        }
        public async Task<int> ExecuteAsync(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                return await connection.ExecuteAsync(qry, dynamicParameters, dbTransaction, commandType: ct);
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }
        public async Task<T?> ExecuteMultipleAsync(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                using (var result = await connection.QueryMultipleAsync(qry, dynamicParameters, dbTransaction, commandType: ct))
                {
                    return (await result.ReadAsync<T>()).SingleOrDefault();
                }
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }   
        }
#if NET9_0_OR_GREATER
        public async Task<int> ExecuteAsync(string qry, CancellationToken cancellationToken, object? dynamicParameters = null, 
            IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                return await connection.ExecuteAsync(qry, dynamicParameters, dbTransaction, commandType: ct)
                    .WaitAsync(cancellationToken);
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }

        public async Task<int> ExecuteUnboundedAsync(string qry, CancellationToken cancellationToken, object? dynamicParameters = null,
            IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                return await connection.ExecuteAsync(new CommandDefinition(qry, dynamicParameters, 
                        dbTransaction, commandType: ct, commandTimeout: 0, cancellationToken: cancellationToken));
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }

        public async Task<T?> ExecuteMultipleAsync(string qry, CancellationToken cancellationToken, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                using (var result = await connection.QueryMultipleAsync(qry, dynamicParameters, dbTransaction, commandType: ct).WaitAsync(cancellationToken))
                {
                    return (await result.ReadAsync<T>()).SingleOrDefault();
                }
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }
        public async Task<TResult?> ExecuteScalarAsync<TResult>(string qry, CancellationToken cancellationToken, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                var task = connection.ExecuteScalarAsync<TResult>(qry, dynamicParameters, dbTransaction, commandType: ct);
                return await task.WaitAsync(cancellationToken);
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }

        public async Task<IEnumerable<TResult>> ExecuteQueryAsync<TResult>(string qry, CancellationToken cancellationToken, object? dynamicParameters = null,
            IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                return await connection.QueryAsync<TResult>(new CommandDefinition(qry, dynamicParameters, dbTransaction, commandType: ct));
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }
        public async Task<TResult> ExecuteQuerySingleAsync<TResult>(string qry, CancellationToken cancellationToken, object? dynamicParameters = null,
            IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                return await connection.QuerySingleAsync<TResult>(new CommandDefinition(qry, dynamicParameters, dbTransaction, commandType: ct));
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }

#endif
        public int Execute(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? GetDbConnection();
            try
            {
                return connection.Execute(qry, dynamicParameters, dbTransaction, commandType: ct);
            }
            finally
            {
                ConnectionDispose.DisposeConnection(dbTransaction, connection);
            }
        }
        public T? ExecuteMultiple(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? GetDbConnection();
            try
            {
                using (var result = connection.QueryMultiple(qry, dynamicParameters, dbTransaction, commandType: ct))
                {
                    return (result.Read<T>()).SingleOrDefault();
                }
            }
            finally
            {
                ConnectionDispose.DisposeConnection(dbTransaction, connection);
            }
        }

    }
}
