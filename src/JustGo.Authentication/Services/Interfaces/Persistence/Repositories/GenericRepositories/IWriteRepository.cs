using System.Data;
using System.Threading.Tasks;

namespace JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories
{
    public interface IWriteRepository<T> where T : class
    {
        Task<int> ExecuteAsync(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
        Task<T?> ExecuteMultipleAsync(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
#if NET9_0_OR_GREATER
        Task<int> ExecuteAsync(string qry, CancellationToken cancellationToken, object? dynamicParameters = null, 
            IDbTransaction? dbTransaction = null, string commandType = "sp");
        Task<int> ExecuteUnboundedAsync(string qry, CancellationToken cancellationToken, object? dynamicParameters = null, 
            IDbTransaction? dbTransaction = null, string commandType = "sp");
        Task<T?> ExecuteMultipleAsync(string qry, CancellationToken cancellationToken, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
        Task<TResult?> ExecuteScalarAsync<TResult>(string qry, CancellationToken cancellationToken, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
        Task<IEnumerable<TResult>> ExecuteQueryAsync<TResult>(string qry, CancellationToken cancellationToken, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
        Task<TResult> ExecuteQuerySingleAsync<TResult>(string qry, CancellationToken cancellationToken, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
#endif        

        int Execute(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
        T? ExecuteMultiple(string qry, object? dynamicParameters = null, IDbTransaction? dbTransaction = null, string commandType = "sp");
    }
}
