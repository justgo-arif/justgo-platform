using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingCatalogDTOs;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetFilterMetaData;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetWebletConfiguration;
using JustGo.Booking.Test.Helper;
using Moq;

namespace JustGo.Booking.Test.Features;

public class GetFilterMetadataHandlerTests
{
    private readonly Mock<IReadRepository<FilterMetadataDto>> _readRepositoryMock;
    private readonly Mock<IReadRepositoryFactory> _readRepositoryFactoryMock;
    private readonly Mock<IHybridCacheService> _cacheServiceMock;
    private readonly GetFilterMetadataHandler _handler;

    public GetFilterMetadataHandlerTests()
    {
        _readRepositoryMock = new Mock<IReadRepository<FilterMetadataDto>>();
        _readRepositoryFactoryMock = new Mock<IReadRepositoryFactory>();
        _cacheServiceMock = new Mock<IHybridCacheService>();
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetWebletConfigurationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebletConfigurationResponse?)null);
        var lazyReadRepository = MockHelperService.MockLazyService(_readRepositoryMock.Object);
        _readRepositoryFactoryMock.Setup(f => f.GetLazyRepository<FilterMetadataDto>()).Returns(lazyReadRepository);
        _handler = new GetFilterMetadataHandler(_readRepositoryFactoryMock.Object, _cacheServiceMock.Object, mediatorMock.Object);
    }

}