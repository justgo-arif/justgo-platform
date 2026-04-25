using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories
{
    public interface IDatabaseProvider
    {
        Task<IDbConnection> GetDbConnectionAsync(bool isRead);
        IDbConnection GetDbConnection(bool isRead);

    }
}
