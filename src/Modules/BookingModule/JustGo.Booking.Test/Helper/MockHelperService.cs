using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Moq;

namespace JustGo.Booking.Test.Helper;

public static class MockHelperService
{
    public static LazyService<T> MockLazyService<T>(T instance) where T : class
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(T))).Returns(instance);

        return new LazyService<T>(serviceProviderMock.Object);
    }

    public static Mock<IReadRepository<T>> MockReadRepository<T>() where T : class
    {
        var mock = new Mock<IReadRepository<T>>(MockBehavior.Strict);
        mock.SetupAllProperties();
        return mock;
    }
}