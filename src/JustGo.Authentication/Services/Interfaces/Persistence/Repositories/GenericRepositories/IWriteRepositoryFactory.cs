using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Helper;

namespace JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories
{
    public interface IWriteRepositoryFactory
    {
        IWriteRepository<T> GetRepository<T>() where T : class;
        LazyService<IWriteRepository<T>> GetLazyRepository<T>() where T : class;
    }
}
