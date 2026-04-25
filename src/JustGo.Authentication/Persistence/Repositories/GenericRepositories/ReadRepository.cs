using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Threading;
using JustGo.Authentication.Helper.Enums;

namespace JustGo.Authentication.Persistence.Repositories.GenericRepositories
{
    public class ReadRepository<T> : IReadRepository<T> where T : class
    {
        private readonly IDatabaseProvider _databaseProvider;
        public ReadRepository(IDatabaseProvider databaseProvider)
        {
            _databaseProvider = databaseProvider;
        }

        private async Task<IDbConnection> GetDbConnectionAsync()
        {
            return await _databaseProvider.GetDbConnectionAsync(true);
        }
        private IDbConnection GetDbConnection()
        {
            return _databaseProvider.GetDbConnection(true);
        }

        //Async

        public async Task<IEnumerable<T>> GetListAsync(string qry, CancellationToken cancellationToken, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
#if NET9_0_OR_GREATER
                return await connection
                    .QueryAsync<T>(qry, dynamicParameters, dbTransaction, commandTimeout: 120, commandType: ct)
                    .WaitAsync(cancellationToken);
#else
                return await connection.QueryAsync<T>(qry, dynamicParameters, dbTransaction, commandTimeout: 120, commandType: ct);
#endif
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }
        public async Task<T?> GetAsync(string qry, CancellationToken cancellationToken, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
#if NET9_0_OR_GREATER
                return await connection.QuerySingleOrDefaultAsync<T>(qry, dynamicParameters, dbTransaction, commandType: ct).WaitAsync(cancellationToken);
#else
                return (await connection.QueryAsync<T>(qry, dynamicParameters, dbTransaction, commandType: ct)).SingleOrDefault();
#endif
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }
        public async Task<object?> GetSingleAsync(string qry, CancellationToken cancellationToken, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
#if NET9_0_OR_GREATER
                return await connection.ExecuteScalarAsync<object>(qry, dynamicParameters, dbTransaction, commandType: ct).WaitAsync(cancellationToken);
#else
                return await connection.ExecuteScalarAsync<object>(qry, dynamicParameters, dbTransaction, commandType: ct);
#endif
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }
//        public async Task<SqlMapper.GridReader> GetMultipleAsync(string qry, CancellationToken cancellationToken, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp")
//        {
//            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
//            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
//#if NET9_0_OR_GREATER
//                return await connection.QueryMultipleAsync(qry, dynamicParameters, dbTransaction, commandType: ct).WaitAsync(cancellationToken);
//#else
//            return await connection.QueryMultipleAsync(qry, dynamicParameters, dbTransaction, commandType: ct);
//#endif            
//        }
        public async Task<IMultipleResultReader> GetMultipleQueryAsync(string qry, CancellationToken cancellationToken, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
#if NET9_0_OR_GREATER
            var gridReader = await connection.QueryMultipleAsync(qry, dynamicParameters, dbTransaction, commandType: ct)
                                             .WaitAsync(cancellationToken);
#else
            var gridReader = await connection.QueryMultipleAsync(qry, dynamicParameters, dbTransaction, commandType: ct);
#endif
            if (dbTransaction is null)
            {
                return new MultipleResultReader(gridReader, connection);
            }
            return new MultipleResultReader(gridReader);
        }
        public async Task<IEnumerable<T>> GetListAsync(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                return await connection.QueryAsync<T>(qry, dynamicParameters, dbTransaction, commandTimeout: 120, commandType: ct);
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }
        public async Task<T?> GetAsync(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
#if NET9_0_OR_GREATER
                return await connection.QuerySingleOrDefaultAsync<T>(qry, dynamicParameters, dbTransaction, commandType: ct);
#else
                return (await connection.QueryAsync<T>(qry, dynamicParameters, dbTransaction, commandType: ct)).SingleOrDefault();
#endif
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }
        public async Task<object?> GetSingleAsync(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                return await connection.ExecuteScalarAsync<object>(qry, dynamicParameters, dbTransaction, commandType: ct);
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }
#if NET9_0_OR_GREATER
        public async Task<TResult?> GetSingleAsync<TResult>(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null,
            CancellationToken cancellationToken = default, string commandType = QueryType.Text)
        {
            var ct = commandType?.ToLower() == QueryType.StoredProcedure ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                return await connection.ExecuteScalarAsync<TResult>(new CommandDefinition(
                    qry, dynamicParameters, dbTransaction, commandType: ct, cancellationToken: cancellationToken
                ));
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }
        public async Task<TResult?> QueryFirstAsync<TResult>(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null,
            string commandType = QueryType.Text, CancellationToken cancellationToken = default)
        {
            var ct = commandType?.ToLower() == QueryType.StoredProcedure ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                return await connection.QueryFirstOrDefaultAsync<TResult>(new CommandDefinition(
                    qry, dynamicParameters, dbTransaction, commandType: ct, cancellationToken: cancellationToken
                ));
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }

        public async Task<IEnumerable<TResult>> GetListAsync<TResult>(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null,
            string commandType = QueryType.Text, CancellationToken cancellationToken = default)
        {
            var ct = commandType?.ToLower() == QueryType.StoredProcedure ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                return await connection.QueryAsync<TResult>(new CommandDefinition(
                    qry, dynamicParameters, dbTransaction, commandType: ct, cancellationToken: cancellationToken
                ));
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }
#endif
        //public async Task<SqlMapper.GridReader> GetMultipleAsync(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp")
        //{
        //    var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
        //    var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
        //    return await connection.QueryMultipleAsync(qry, dynamicParameters, dbTransaction, commandType: ct);
        //}
#if NET9_0_OR_GREATER
        public async Task<List<T1>> GetListMultiMappingAsync<T1, T2>(string qry, CancellationToken cancellationToken, string t1PrimaryKey, Func<T1, T2, T1> map, object? dynamicParameters = null
            , IDbTransaction? dbTransaction = null, string splitOn = "Id", string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                var dictionary = new Dictionary<object, T1>();

                var result = await connection.QueryAsync<T1, T2, T1>(
                    qry,
                    (t1, t2) =>
                    {
                        var key = t1?.GetType().GetProperty(t1PrimaryKey)?.GetValue(t1, null)?.ToString();
                        if (key == null)
                        {
                            if (!dictionary.TryGetValue(key, out var entity))
                            {
                                entity = t1;
                                dictionary.Add(key, entity);
                            }

                            return map(entity, t2);
                        }
                        return map(t1, t2);
                    },
                    dynamicParameters,
                    dbTransaction,
                    commandTimeout: 120,
                    splitOn: splitOn,
                    commandType: ct
                ).WaitAsync(cancellationToken);
                return result.Distinct().ToList();
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }
        public async Task<List<T1>> GetListMultiMappingAsync<T1, T2, T3>(string qry, CancellationToken cancellationToken, string t1PrimaryKey, Func<T1, T2, T3, T1> map, object? dynamicParameters = null
            , IDbTransaction? dbTransaction = null, string splitOn = "Id", string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                var dictionary = new Dictionary<object, T1>();

                var result = await connection.QueryAsync<T1, T2, T3, T1>(
                    qry,
                    (t1, t2, t3) =>
                    {
                        var key = t1?.GetType().GetProperty(t1PrimaryKey)?.GetValue(t1, null)?.ToString();
                        if (key != null)
                        {
                            if (!dictionary.TryGetValue(key, out var entity))
                            {
                                entity = t1;
                                dictionary.Add(key, entity);
                            }

                            return map(entity, t2, t3);
                        }
                        return map(t1, t2, t3);
                    },
                    dynamicParameters,
                    dbTransaction,
                    commandTimeout: 120,
                    splitOn: splitOn,
                    commandType: ct
                ).WaitAsync(cancellationToken);
                return result.Distinct().ToList();
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }
        public async Task<List<T1>> GetListMultiMappingAsync<T1, T2, T3, T4>(string qry, CancellationToken cancellationToken, string t1PrimaryKey, Func<T1, T2, T3, T4, T1> map, object? dynamicParameters = null
            , IDbTransaction? dbTransaction = null, string splitOn = "Id", string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                var dictionary = new Dictionary<object, T1>();

                var result = await connection.QueryAsync<T1, T2, T3, T4, T1>(
                    qry,
                    (t1, t2, t3, t4) =>
                    {
                        var key = t1?.GetType().GetProperty(t1PrimaryKey)?.GetValue(t1, null)?.ToString();
                        if (key != null)
                        {
                            if (!dictionary.TryGetValue(key, out var entity))
                            {
                                entity = t1;
                                dictionary.Add(key, entity);
                            }

                            return map(entity, t2, t3, t4);
                        }
                        return map(t1, t2, t3, t4);
                    },
                    dynamicParameters,
                    dbTransaction,
                    commandTimeout: 120,
                    splitOn: splitOn,
                    commandType: ct
                ).WaitAsync(cancellationToken);
                return result.Distinct().ToList();
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }
        public async Task<T1?> GetMultiMappingAsync<T1, T2>(string qry, CancellationToken cancellationToken, string t1PrimaryKey, Func<T1, T2, T1> map, object? dynamicParameters = null
            , IDbTransaction? dbTransaction = null, string splitOn = "Id", string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                var dictionary = new Dictionary<object, T1>();

                var result = await connection.QueryAsync<T1, T2, T1>(
                    qry,
                    (t1, t2) =>
                    {
                        var key = t1?.GetType().GetProperty(t1PrimaryKey)?.GetValue(t1, null)?.ToString();
                        if (key != null)
                        {
                            if (!dictionary.TryGetValue(key, out var entity))
                            {
                                entity = t1;
                                dictionary.Add(key, entity);
                            }

                            return map(entity, t2);
                        }
                        return map(t1, t2);
                    },
                    dynamicParameters,
                    dbTransaction,
                    commandTimeout: 120,
                    splitOn: splitOn,
                    commandType: ct
                ).WaitAsync(cancellationToken);
                return result.FirstOrDefault();
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }
        public async Task<T1?> GetMultiMappingAsync<T1, T2, T3>(string qry, CancellationToken cancellationToken, string t1PrimaryKey, Func<T1, T2, T3, T1> map, object? dynamicParameters = null
            , IDbTransaction? dbTransaction = null, string splitOn = "Id", string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                var dictionary = new Dictionary<object, T1>();

                var result = await connection.QueryAsync<T1, T2, T3, T1>(
                    qry,
                    (t1, t2, t3) =>
                    {
                        var key = t1?.GetType().GetProperty(t1PrimaryKey)?.GetValue(t1, null)?.ToString();
                        if (key != null)
                        {
                            if (!dictionary.TryGetValue(key, out var entity))
                            {
                                entity = t1;
                                dictionary.Add(key, entity);
                            }

                            return map(entity, t2, t3);
                        }
                        return map(t1, t2, t3);
                    },
                    dynamicParameters,
                    dbTransaction,
                    commandTimeout: 120,
                    splitOn: splitOn,
                    commandType: ct
                ).WaitAsync(cancellationToken);
                return result.FirstOrDefault();
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }
        public async Task<T1?> GetMultiMappingAsync<T1, T2, T3, T4>(string qry, CancellationToken cancellationToken, string t1PrimaryKey, Func<T1, T2, T3, T4, T1> map, object? dynamicParameters = null
            , IDbTransaction? dbTransaction = null, string splitOn = "Id", string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? await GetDbConnectionAsync();
            try
            {
                var dictionary = new Dictionary<object, T1>();

                var result = await connection.QueryAsync<T1, T2, T3, T4, T1>(
                    qry,
                    (t1, t2, t3, t4) =>
                    {
                        var key = t1?.GetType().GetProperty(t1PrimaryKey)?.GetValue(t1, null)?.ToString();
                        if (key != null)
                        {
                            if (!dictionary.TryGetValue(key, out var entity))
                            {
                                entity = t1;
                                dictionary.Add(key, entity);
                            }

                            return map(entity, t2, t3, t4);
                        }
                        return map(t1, t2, t3, t4);
                    },
                    dynamicParameters,
                    dbTransaction,
                    commandTimeout: 120,
                    splitOn: splitOn,
                    commandType: ct
                ).WaitAsync(cancellationToken);
                return result.FirstOrDefault();
            }
            finally
            {
                await ConnectionDispose.DisposeConnectionAsync(dbTransaction, connection);
            }
        }        
#endif
        //Sync
        public IEnumerable<T> GetList(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? GetDbConnection();
            try
            {
                return connection.Query<T>(qry, dynamicParameters, dbTransaction, commandTimeout: 120, commandType: ct);
            }
            finally
            {
                ConnectionDispose.DisposeConnection(dbTransaction, connection);
            }
        }
        public T? Get(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? GetDbConnection();
            try
            {
#if NET9_0_OR_GREATER
                return connection.QuerySingleOrDefault<T>(qry, dynamicParameters, dbTransaction, commandType: ct);
#else
                return connection.Query<T>(qry, dynamicParameters, dbTransaction, commandType: ct).SingleOrDefault();
#endif
            }
            finally
            {
                ConnectionDispose.DisposeConnection(dbTransaction, connection);
            }
        }
        public object? GetSingle(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? GetDbConnection();
            try
            {
                return connection.ExecuteScalar<object>(qry, dynamicParameters, dbTransaction, commandType: ct);
            }
            finally
            {
                ConnectionDispose.DisposeConnection(dbTransaction, connection);
            }
        }
        public SqlMapper.GridReader GetMultipleQuery(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp")
        {
            var ct = commandType?.ToLower() == "sp" ? CommandType.StoredProcedure : CommandType.Text;
            var connection = dbTransaction?.Connection ?? GetDbConnection();

            return connection.QueryMultiple(qry, dynamicParameters, dbTransaction, commandType: ct);
        }
        


    }
}
