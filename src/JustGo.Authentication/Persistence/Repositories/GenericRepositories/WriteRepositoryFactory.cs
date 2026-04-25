using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Microsoft.Extensions.DependencyInjection;

namespace JustGo.Authentication.Persistence.Repositories.GenericRepositories
{
    public class WriteRepositoryFactory : IWriteRepositoryFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public WriteRepositoryFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IWriteRepository<T> GetRepository<T>() where T : class
        {
            return _serviceProvider.GetRequiredService<IWriteRepository<T>>();
        }
        public LazyService<IWriteRepository<T>> GetLazyRepository<T>() where T : class
        {
            return _serviceProvider.GetRequiredService<LazyService<IWriteRepository<T>>>();
        }
    }
}
