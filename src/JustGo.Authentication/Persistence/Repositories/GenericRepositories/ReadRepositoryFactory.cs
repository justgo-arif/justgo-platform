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
    public class ReadRepositoryFactory : IReadRepositoryFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ReadRepositoryFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IReadRepository<T> GetRepository<T>() where T : class
        {
            return _serviceProvider.GetRequiredService<IReadRepository<T>>();
        }
        public LazyService<IReadRepository<T>> GetLazyRepository<T>() where T : class
        {
            return _serviceProvider.GetRequiredService<LazyService<IReadRepository<T>>>();
        }
    }
}
