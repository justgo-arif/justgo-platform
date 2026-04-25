using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace JustGo.Authentication.Helper
{
    public class LazyService<T> : Lazy<T>
    {
        public LazyService(IServiceProvider serviceProvider)
           : base(() => serviceProvider.GetRequiredService<T>())
        {
        }
    }
}
