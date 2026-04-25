using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Helper;
using Moq;

namespace JustGo.MemberProfile.Test.Helper
{
    public static class LazyServiceMockHelper
    {
        public static LazyService<T> MockLazyService<T>(T instance) where T : class
        {
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(typeof(T))).Returns(instance);

            return new LazyService<T>(serviceProviderMock.Object);
        }
    }
}
