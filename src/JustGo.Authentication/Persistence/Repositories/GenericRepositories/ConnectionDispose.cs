using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Persistence.Repositories.GenericRepositories
{
    public static class ConnectionDispose
    {
        public static async Task DisposeConnectionAsync(IDbTransaction? dbTransaction, IDbConnection connection)
        {
            if (dbTransaction is null)
            {
                if (connection is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync();
                else
                    connection.Dispose();
            }
        }
        public static void DisposeConnection(IDbTransaction? dbTransaction, IDbConnection connection)
        {
            if (dbTransaction is null)
            {
                connection.Dispose();
            }
        }
    }
}
