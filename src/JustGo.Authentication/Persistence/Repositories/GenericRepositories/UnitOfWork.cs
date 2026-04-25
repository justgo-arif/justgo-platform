using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;

namespace JustGo.Authentication.Persistence.Repositories.GenericRepositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDatabaseProvider _databaseProvider;
        private IDbConnection? _connection;
        private bool _disposed;
        public UnitOfWork(IDatabaseProvider databaseProvider)
        {
            _databaseProvider = databaseProvider;
        }

        private async Task<IDbConnection> GetDbConnectionAsync()
        {
            if (_connection is null)
            {
                _connection = await _databaseProvider.GetDbConnectionAsync(false);
            }
            return _connection;
        }
        private IDbConnection GetDbConnection()
        {
            if (_connection is null)
            {
                _connection = _databaseProvider.GetDbConnection(false);
            }
            return _connection;
        }

        public async Task<IDbTransaction> BeginTransactionAsync()
        {
            var connection = await GetDbConnectionAsync();
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
            return connection.BeginTransaction();
        }
        
#if NET9_0_OR_GREATER
        public async Task<DbTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        {
            var connection = await GetDbConnectionAsync();
    
            if (connection == null)
            {
                throw new InvalidOperationException("Database connection cannot be null");
            }

            if (connection is not DbConnection dbConnection)
            {
                throw new InvalidOperationException("Connection must be a DbConnection for async transaction support");
            }

            try
            {
                if (dbConnection.State == ConnectionState.Closed)
                {
                    await dbConnection.OpenAsync(cancellationToken);
                }
        
                var transaction = await dbConnection.BeginTransactionAsync(cancellationToken);
                if (transaction == null)
                {
                    throw new InvalidOperationException("Failed to create database transaction");
                }
        
                return transaction;
            }
            catch
            {
                if (dbConnection.State == ConnectionState.Open)
                {
                    await dbConnection.CloseAsync();
                }
                throw;
            }
        }
#endif
        public async Task CommitAsync(IDbTransaction transaction)
        {
            try
            {
                transaction.Commit();
            }
            finally
            {
                await DisposeConnectionAsync(transaction.Connection);
            }
        }

        public async Task RollbackAsync(IDbTransaction transaction)
        {
            try
            {
                transaction.Rollback();
            }
            finally
            {
                await DisposeConnectionAsync(transaction.Connection);
            }
        }

        private async Task DisposeConnectionAsync(IDbConnection? connection)
        {
            if (connection != null && connection.State != ConnectionState.Closed)
            {
                connection.Close();
            }
            if (connection is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else
                connection?.Dispose();
        }

        public IDbTransaction BeginTransaction()
        {
            var connection = GetDbConnection();
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
            return connection.BeginTransaction();
        }

        public void Commit(IDbTransaction transaction)
        {
            try
            {
                transaction.Commit();
            }
            finally
            {
                DisposeConnection(transaction.Connection);
            }
        }

        public void Rollback(IDbTransaction transaction)
        {
            try
            {
                transaction.Rollback();
            }
            finally
            {
                DisposeConnection(transaction.Connection);
            }
        }

        private void DisposeConnection(IDbConnection? connection)
        {
            if (connection != null && connection.State != ConnectionState.Closed)
            {
                connection.Dispose();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                DisposeConnection(_connection);
                _disposed = true;
            }
        }


        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                await DisposeConnectionAsync(_connection);
                _disposed = true;
            }
        }



    }
}
