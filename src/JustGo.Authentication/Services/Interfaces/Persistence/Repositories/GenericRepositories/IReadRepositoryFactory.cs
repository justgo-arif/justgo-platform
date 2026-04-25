using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Helper;

namespace JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories
{
    public interface IReadRepositoryFactory
    {
        IReadRepository<T> GetRepository<T>() where T : class;
        LazyService<IReadRepository<T>> GetLazyRepository<T>() where T : class;
    }
}
