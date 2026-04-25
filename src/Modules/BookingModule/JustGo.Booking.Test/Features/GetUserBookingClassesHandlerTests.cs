using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetUserBookingClasses;
using JustGo.Booking.Domain.Entities;
using Moq;
using System.Data;

namespace JustGo.Booking.Test.Features;

public class GetUserBookingClassesHandlerTests
{
    private readonly Mock<IReadRepositoryFactory> _readRepositoryFactoryMock;
    private readonly Mock<IReadRepository<MemberClass>> _memberClassRepositoryMock;
    private readonly LazyService<IReadRepository<MemberClass>> _lazyMemberClassRepository;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly GetUserBookingClassesHandler _handler;

    public GetUserBookingClassesHandlerTests()
    {
        _readRepositoryFactoryMock = new Mock<IReadRepositoryFactory>();
        _memberClassRepositoryMock = new Mock<IReadRepository<MemberClass>>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IReadRepository<MemberClass>)))
            .Returns(_memberClassRepositoryMock.Object);

        _lazyMemberClassRepository = new LazyService<IReadRepository<MemberClass>>(_serviceProviderMock.Object);

        _readRepositoryFactoryMock
            .Setup(x => x.GetLazyRepository<MemberClass>())
            .Returns(_lazyMemberClassRepository);

        _handler = new GetUserBookingClassesHandler(_readRepositoryFactoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnPresentClasses_WhenIsPastIsFalse()
    {
        // Arrange
        var query = new GetUserBookingClassesQuery { UserGuid = Guid.NewGuid(), IsPast = false, NumberOfRow = 10, LastSeenId = 0 };
        var memberClasses = new List<MemberClass>
        {
            new()
            {
                ClassGroupName = "Group1",
                ClassName = "Class1",
                ClassGuid = Guid.NewGuid().ToString(),
                VenueName = "Venue1",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(1).AddHours(1),
                BookingAmount = 10,
                ProductType = 1,
                TotalRows = 1,
                RowNumber = 1
            }
        };

        _memberClassRepositoryMock
            .Setup(x => x.GetListAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                It.IsAny<string>()))
            .ReturnsAsync(memberClasses);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.False(result.HasMore);
        Assert.Equal("Class1", result.Items.First().ClassName);
    }

    [Fact]
    public async Task Handle_ShouldReturnPastClasses_WhenIsPastIsTrue()
    {
        // Arrange
        var query = new GetUserBookingClassesQuery { UserGuid = Guid.NewGuid(), IsPast = true, NumberOfRow = 10, LastSeenId = 0 };
        var memberClasses = new List<MemberClass>
        {
            new()
            {
                ClassGroupName = "GroupOld",
                ClassName = "ClassOld",
                ClassGuid = Guid.NewGuid().ToString(),
                VenueName = "VenueOld",
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddDays(-10).AddHours(1),
                BookingAmount = 10,
                ProductType = 1,
                TotalRows = 1,
                RowNumber = 1
            }
        };

        _memberClassRepositoryMock
            .Setup(x => x.GetListAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                It.IsAny<string>()))
            .ReturnsAsync(memberClasses);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("ClassOld", result.Items.First().ClassName);
    }

    [Fact]
    public async Task Handle_ShouldHandlePagination_WhenMoreRowsExist()
    {
        // Arrange
        var query = new GetUserBookingClassesQuery { UserGuid = Guid.NewGuid(), IsPast = false, NumberOfRow = 1, LastSeenId = 0 };
        var memberClasses = new List<MemberClass>
        {
            new()
            {
                ClassGroupName = "Group1",
                ClassName = "Class1",
                ClassGuid = Guid.NewGuid().ToString(),
                VenueName = "Venue1",
                StartDate = DateTime.UtcNow.AddDays(1),
                EndDate = DateTime.UtcNow.AddDays(1).AddHours(1),
                BookingAmount = 10,
                ProductType = 1,
                TotalRows = 2,
                RowNumber = 1
            },
            new()
            {
                ClassGroupName = "Group2",
                ClassName = "Class2",
                ClassGuid = Guid.NewGuid().ToString(),
                VenueName = "Venue2",
                StartDate = DateTime.UtcNow.AddDays(2),
                EndDate = DateTime.UtcNow.AddDays(2).AddHours(1),
                BookingAmount = 10,
                ProductType = 1,
                TotalRows = 2,
                RowNumber = 2
            }
        };

        // Mock returns 2 items, but we requested NumberOfRow = 1.
        // Handler fetches NumberOfRow + 1 internally.

        _memberClassRepositoryMock
            .Setup(x => x.GetListAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                It.IsAny<string>()))
            .ReturnsAsync(memberClasses);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items); // Should remove the extra item
        Assert.True(result.HasMore);
        Assert.Equal("Class1", result.Items.First().ClassName);
    }

}
