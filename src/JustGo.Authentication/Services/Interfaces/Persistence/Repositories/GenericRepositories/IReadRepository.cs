using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using System.Threading;
using JustGo.Authentication.Helper.Enums;

namespace JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories
{
    public interface IReadRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetListAsync(string qry, CancellationToken cancellationToken, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
        Task<T?> GetAsync(string qry, CancellationToken cancellationToken, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
        Task<object?> GetSingleAsync(string qry, CancellationToken cancellationToken, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
        //Task<SqlMapper.GridReader> GetMultipleAsync(string qry, CancellationToken cancellationToken, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
        Task<IMultipleResultReader> GetMultipleQueryAsync(string qry, CancellationToken cancellationToken, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
        Task<IEnumerable<T>> GetListAsync(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
        Task<T?> GetAsync(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
        Task<object?> GetSingleAsync(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
        //Task<SqlMapper.GridReader> GetMultipleAsync(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
#if NET9_0_OR_GREATER
        Task<TResult?> GetSingleAsync<TResult>(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, CancellationToken cancellationToken = default, string commandType = QueryType.Text);
        Task<TResult?> QueryFirstAsync<TResult>(string qry, object? dynamicParameters = null,
            IDbTransaction? dbTransaction = null,
            string commandType = QueryType.Text,
            CancellationToken cancellationToken = default);
        Task<IEnumerable<TResult>> GetListAsync<TResult>(string qry, object? dynamicParameters = null,
            IDbTransaction? dbTransaction = null, string commandType = QueryType.Text,
            CancellationToken cancellationToken = default);
        Task<List<T1>> GetListMultiMappingAsync<T1, T2>(string qry, CancellationToken cancellationToken, string t1PrimaryKey, Func<T1, T2, T1> map, object? dynamicParameters = null
            , IDbTransaction? dbTransaction = null, string splitOn = "Id", string commandType = "sp");
        Task<List<T1>> GetListMultiMappingAsync<T1, T2, T3>(string qry, CancellationToken cancellationToken, string t1PrimaryKey, Func<T1, T2, T3, T1> map, object? dynamicParameters = null
            , IDbTransaction? dbTransaction = null, string splitOn = "Id", string commandType = "sp");
        Task<List<T1>> GetListMultiMappingAsync<T1, T2, T3, T4>(string qry, CancellationToken cancellationToken, string t1PrimaryKey, Func<T1, T2, T3, T4, T1> map, object? dynamicParameters = null
            , IDbTransaction? dbTransaction = null, string splitOn = "Id", string commandType = "sp");
        Task<T1?> GetMultiMappingAsync<T1, T2>(string qry, CancellationToken cancellationToken, string t1PrimaryKey, Func<T1, T2, T1> map, object? dynamicParameters = null
            , IDbTransaction? dbTransaction = null, string splitOn = "Id", string commandType = "sp");
        Task<T1?> GetMultiMappingAsync<T1, T2, T3>(string qry, CancellationToken cancellationToken, string t1PrimaryKey, Func<T1, T2, T3, T1> map, object? dynamicParameters = null
            , IDbTransaction? dbTransaction = null, string splitOn = "Id", string commandType = "sp");
        Task<T1?> GetMultiMappingAsync<T1, T2, T3, T4>(string qry, CancellationToken cancellationToken, string t1PrimaryKey, Func<T1, T2, T3, T4, T1> map, object? dynamicParameters = null
            , IDbTransaction? dbTransaction = null, string splitOn = "Id", string commandType = "sp");
#endif
        IEnumerable<T> GetList(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
        T? Get(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
        object? GetSingle(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
        SqlMapper.GridReader GetMultipleQuery(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");

      

    }
}
