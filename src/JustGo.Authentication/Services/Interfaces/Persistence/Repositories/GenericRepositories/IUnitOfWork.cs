using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        Task<IDbTransaction> BeginTransactionAsync();
        Task CommitAsync(IDbTransaction transaction);
        Task RollbackAsync(IDbTransaction transaction);

        IDbTransaction BeginTransaction();
        void Commit(IDbTransaction transaction);
        void Rollback(IDbTransaction transaction);

#if NET9_0_OR_GREATER 
        Task<DbTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
#endif
    }
}
