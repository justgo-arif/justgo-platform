using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetClasses;
using JustGo.Booking.Domain.Entities;
using JustGo.Booking.Test.Helper;
using Moq;

namespace JustGo.Booking.Test.Features;

public class GetClassesBySyncGuidHandlerTests
{
    private readonly Mock<IReadRepository<BookingClass>> _readRepositoryMock;
    private readonly Mock<IReadRepository<BookingSession>> _readSessionRepositoryMock;
    private readonly Mock<IReadRepositoryFactory> _readRepositoryFactoryMock;
    private readonly Mock<IHybridCacheService> _cacheServiceMock;
    private readonly GetClassesBySyncGuidHandler _handler;
    private readonly Mock<IMediator> _mediatorMock;

    public GetClassesBySyncGuidHandlerTests()
    {
        _readRepositoryMock = new Mock<IReadRepository<BookingClass>>();
        _readSessionRepositoryMock = new Mock<IReadRepository<BookingSession>>();
        _readRepositoryFactoryMock = new Mock<IReadRepositoryFactory>();
        _cacheServiceMock = new Mock<IHybridCacheService>();
        _mediatorMock = new Mock<IMediator>();

        var lazyreadRepo = MockHelperService.MockLazyService(_readRepositoryMock.Object);
        _readRepositoryFactoryMock.Setup(f => f.GetLazyRepository<BookingClass>()).Returns(lazyreadRepo);

        var lazySessionRepo = MockHelperService.MockLazyService(_readSessionRepositoryMock.Object);
        _readRepositoryFactoryMock.Setup(f => f.GetLazyRepository<BookingSession>()).Returns(lazySessionRepo);

        _readSessionRepositoryMock
            .Setup(r => r.GetListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), null, "text"))
            .ReturnsAsync(new List<BookingSession>());

        _handler = new GetClassesBySyncGuidHandler(_readRepositoryFactoryMock.Object, _cacheServiceMock.Object, _mediatorMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResult_WithExpectedItems()
    {
        // Arrange
        var query = new GetClassesBySyncGuidQuery
        {
            OwningEntityGuid = Guid.NewGuid(),
            NumberOfRow = 2,
            SortBy = "Day",
            OrderBy = "ASC"
        };

        var bookingClasses = new List<BookingClass>
        {
            new BookingClass
            {
                SessionName = "Session 1",
                SessionGuid = Guid.NewGuid().ToString(),
                Capacity = 10,
                ClassName = "Class 1",
                ClassGuid = Guid.NewGuid().ToString(),
                CategoryName = "Category 1",
                AgeGroupName = "Age group 1",
                OwningEntitySyncGuid = query.OwningEntityGuid.ToString(),
                MinAge = 5,
                MaxAge = 10,
                Gender = "Male,Female",
                ColorName = "Red",
                ColorCode = "#FF0000",
                ClassImages = "img1.jpg|img2.jpg",
                OneOffPrice = 100,
                MonthlyPrice = 200,
                PaygPrice = 50,
                ScheduleInfo = "Mon|09:00|10:00,Tue|10:00|11:00",
                TotalRows = 3,
                RowNumber = 1
            },
            new BookingClass
            {
                SessionName = "Session 2",
                SessionGuid = Guid.NewGuid().ToString(),
                Capacity = 15,
                ClassName = "Class 2",
                ClassGuid = Guid.NewGuid().ToString(),
                CategoryName = "Category 2",
                AgeGroupName = "Age group 2",
                OwningEntitySyncGuid = query.OwningEntityGuid.ToString(),
                MinAge = 8,
                MaxAge = 12,
                Gender = "Female",
                ColorName = "Blue",
                ColorCode = "#0000FF",
                ClassImages = "img3.jpg",
                OneOffPrice = 120,
                MonthlyPrice = 220,
                PaygPrice = 60,
                ScheduleInfo = "Wed|11:00|12:00",
                TotalRows = 3,
                RowNumber = 2
            },
            // Simulate extra row for HasMore
            new BookingClass
            {
                SessionName = "Session 3",
                SessionGuid = Guid.NewGuid().ToString(),
                Capacity = 20,
                ClassName = "Class 3",
                ClassGuid = Guid.NewGuid().ToString(),
                CategoryName = "Category 3",
                AgeGroupName = "Age group 3",
                OwningEntitySyncGuid = query.OwningEntityGuid.ToString(),
                MinAge = 10,
                MaxAge = 15,
                Gender = "Male",
                ColorName = "Green",
                ColorCode = "#00FF00",
                ClassImages = "img4.jpg",
                OneOffPrice = 130,
                MonthlyPrice = 230,
                PaygPrice = 70,
                ScheduleInfo = "Thu|12:00|13:00",
                TotalRows = 3,
                RowNumber = 3
            }
        };

        // Setup the cache to execute the factory function, simulating a cache miss
        _cacheServiceMock.Setup(c => c.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<List<BookingClass>>>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<string[]>(),
            It.IsAny<CancellationToken>()))
        .Returns(async (string key, Func<CancellationToken, Task<List<BookingClass>>> factory, TimeSpan duration, string[] tags, CancellationToken token) => await factory(token));

        // Setup the repository mock to return the bookingClasses list
        _readRepositoryMock
            .Setup(r => r.GetListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), null, "text"))
            .ReturnsAsync(bookingClasses);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count); // Should only return NumberOfRow items
        Assert.True(result.HasMore); // Because there was an extra row
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.LastSeenId);

        var first = result.Items[0];
        Assert.Equal("Session 1", first.SessionName);
        Assert.Equal("Class 1", first.ClassName);
        Assert.Equal(new[] { "Male", "Female" }, first.Gender);
        Assert.Equal(new[] { "img1.jpg", "img2.jpg" }, first.ClassImages);
        Assert.Equal(2, first.ScheduleInfo.Count);

        var second = result.Items[1];
        Assert.Equal("Session 2", second.SessionName);
        Assert.Equal("Class 2", second.ClassName);
        Assert.Equal(new[] { "Female" }, second.Gender);
        Assert.Equal(new[] { "img3.jpg" }, second.ClassImages);
        Assert.Single(second.ScheduleInfo);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyResult_WhenNoClasses()
    {
        // Arrange
        var query = new GetClassesBySyncGuidQuery
        {
            OwningEntityGuid = Guid.NewGuid(),
            NumberOfRow = 2,
            SortBy = "Day",
            OrderBy = "ASC"
        };

        _cacheServiceMock.Setup(c => c.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<List<BookingClass>>>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<string[]>(),
            It.IsAny<CancellationToken>()))
        .Returns(async (string key, Func<CancellationToken, Task<List<BookingClass>>> factory, TimeSpan duration, string[] tags, CancellationToken token) => await factory(token));

        _readRepositoryMock
            .Setup(r => r.GetListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), null, "text"))
            .ReturnsAsync(new List<BookingClass>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.False(result.HasMore);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.LastSeenId);
    }

    [Fact]
    public async Task Handle_UsesTotalRowsFromRequest_WhenProvided()
    {
        // Arrange
        var query = new GetClassesBySyncGuidQuery
        {
            OwningEntityGuid = Guid.NewGuid(),
            NumberOfRow = 1,
            TotalRows = 99,
            SortBy = "Day",
            OrderBy = "ASC"
        };

        var bookingClasses = new List<BookingClass>
        {
            new BookingClass
            {
                SessionName = "Session 1",
                SessionGuid = Guid.NewGuid().ToString(),
                Capacity = 10,
                ClassName = "Class 1",
                ClassGuid = Guid.NewGuid().ToString(),
                CategoryName = "Category 1",
                AgeGroupName = "Age group 1",
                OwningEntitySyncGuid = query.OwningEntityGuid.ToString(),
                MinAge = 5,
                MaxAge = 10,
                Gender = "Male",
                ColorName = "Red",
                ColorCode = "#FF0000",
                ClassImages = "img1.jpg",
                OneOffPrice = 100,
                MonthlyPrice = 200,
                PaygPrice = 50,
                ScheduleInfo = "Mon|09:00|10:00",
                TotalRows = 2,
                RowNumber = 1
            },
            // Simulate extra row for HasMore
            new BookingClass
            {
                SessionName = "Session 2",
                SessionGuid = Guid.NewGuid().ToString(),
                Capacity = 15,
                ClassName = "Class 2",
                ClassGuid = Guid.NewGuid().ToString(),
                CategoryName = "Category 2",
                AgeGroupName = "Age group 2",
                OwningEntitySyncGuid = query.OwningEntityGuid.ToString(),
                MinAge = 8,
                MaxAge = 12,
                Gender = "Female",
                ColorName = "Blue",
                ColorCode = "#0000FF",
                ClassImages = "img2.jpg",
                OneOffPrice = 120,
                MonthlyPrice = 220,
                PaygPrice = 60,
                ScheduleInfo = "Tue|10:00|11:00",
                TotalRows = 2,
                RowNumber = 2
            }
        };

        _cacheServiceMock.Setup(c => c.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<List<BookingClass>>>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<string[]>(),
            It.IsAny<CancellationToken>()))
        .Returns(async (string key, Func<CancellationToken, Task<List<BookingClass>>> factory, TimeSpan duration, string[] tags, CancellationToken token) => await factory(token));

        _readRepositoryMock
            .Setup(r => r.GetListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), null, "text"))
            .ReturnsAsync(bookingClasses);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.True(result.HasMore);
        Assert.Equal(99, result.TotalCount); // Should use TotalRows from request
        Assert.Equal(1, result.LastSeenId);
    }
}