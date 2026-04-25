using Dapper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.BookingCatalogDTOs;
using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetAgeGroups;
using JustGo.Booking.Application.Features.BookingCatalogs.Queries.GetDisciplines;
using JustGo.Booking.Application.Features.BookingClasses.Queries.GetWebletConfiguration;
using JustGo.Booking.Test.Helper;
using Moq;

namespace JustGo.Booking.Test.Features;

public class GetBookingCategoriesTests
{
    private readonly Mock<IReadRepositoryFactory> _readRepositoryFactoryMock;
    private readonly Mock<IHybridCacheService> _cacheServiceMock;

    private readonly Mock<IReadRepository<DisciplineCategoryDto>> _disciplineCategoryReadRepositoryMock;
    private readonly Mock<IReadRepository<AgeGroupCategoryDto>> _ageGroupCategoryReadRepositoryMock;
    //private readonly Mock<IReadRepository<BasicClubDetailDto>> _basicClubDetailReadRepositoryMock;

    private readonly GetDisciplinesBySyncGuidHandler _disciplineHandler;
    private readonly GetAgeGroupsBySyncGuidHandler _ageGroupHandler;
    //private readonly GetBasicClubDetailBySyncGuidHandler _basicClubDetailHandler;

    public GetBookingCategoriesTests()
    {
        _readRepositoryFactoryMock = new Mock<IReadRepositoryFactory>();
        _cacheServiceMock = new Mock<IHybridCacheService>();
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetWebletConfigurationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebletConfigurationResponse?)null);

        _disciplineCategoryReadRepositoryMock = new Mock<IReadRepository<DisciplineCategoryDto>>();
        var lazyReadDisciplineCategoryRepo = MockHelperService.MockLazyService(_disciplineCategoryReadRepositoryMock.Object);
        _readRepositoryFactoryMock.Setup(f => f.GetLazyRepository<DisciplineCategoryDto>()).Returns(lazyReadDisciplineCategoryRepo);

        _disciplineHandler = new GetDisciplinesBySyncGuidHandler(_readRepositoryFactoryMock.Object, _cacheServiceMock.Object,mediatorMock.Object);

        _ageGroupCategoryReadRepositoryMock = new Mock<IReadRepository<AgeGroupCategoryDto>>();
        var lazyReadAgeGroupCategoryRepo = MockHelperService.MockLazyService(_ageGroupCategoryReadRepositoryMock.Object);
        _readRepositoryFactoryMock.Setup(f => f.GetLazyRepository<AgeGroupCategoryDto>()).Returns(lazyReadAgeGroupCategoryRepo);

        _ageGroupHandler = new GetAgeGroupsBySyncGuidHandler(_readRepositoryFactoryMock.Object, _cacheServiceMock.Object,mediatorMock.Object);

        //_basicClubDetailReadRepositoryMock = new Mock<IReadRepository<BasicClubDetailDto>>();
        //var lazyReadBasicClubDetailRepo = MockHelperService.MockLazyService(_basicClubDetailReadRepositoryMock.Object);
        //_readRepositoryFactoryMock.Setup(f => f.GetLazyRepository<BasicClubDetailDto>()).Returns(lazyReadBasicClubDetailRepo);
        //_basicClubDetailHandler = new GetBasicClubDetailBySyncGuidHandler(_readRepositoryFactoryMock.Object, _cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnDisciplineCategories_WhenDataExists()
    {
        // Arrange
        var syncGuid = Guid.NewGuid();
        var webletGuid = Guid.NewGuid();
        var query = new GetDisciplinesBySyncGuidQuery(syncGuid, webletGuid);

        var mockData = new List<DisciplineCategoryDto>
        {
            new()
            {
                CategoryGuid = Guid.NewGuid().ToString(),
                Name = "Football",
                ImageUrl = "football.jpg"
            },
            new()
            {
                CategoryGuid = Guid.NewGuid().ToString(),
                Name = "Basketball",
                ImageUrl = "basketball.jpg"
            }
        };

        // Setup the cache to execute the factory function, simulating a cache miss
        _cacheServiceMock.Setup(c => c.GetOrSetAsync(
            It.IsAny<string>(),
            It.IsAny<Func<CancellationToken, Task<IEnumerable<DisciplineCategoryDto>>>>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<string[]>(),
            It.IsAny<CancellationToken>()))
        .Returns(async (string key, Func<CancellationToken, Task<IEnumerable<DisciplineCategoryDto>>> factory, TimeSpan duration, string[] tags, CancellationToken token) => await factory(token));

        // Setup the repository mock to return the bookingClasses list
        _disciplineCategoryReadRepositoryMock
            .Setup(r => r.GetListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), null, "text"))
            .ReturnsAsync(mockData);

        // Act
        var result = await _disciplineHandler.Handle(query, CancellationToken.None);

        // Assert
        var disciplineCategoryDtos = result as DisciplineCategoryDto[] ?? result.ToArray();
        disciplineCategoryDtos.Should().NotBeNull();
        disciplineCategoryDtos.Should().HaveCount(2);
        disciplineCategoryDtos.First().Name.Should().Be("Football");
        disciplineCategoryDtos.Last().Name.Should().Be("Basketball");

        _disciplineCategoryReadRepositoryMock.Verify(r => r.GetListAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            It.Is<DynamicParameters>(p => p.Get<string>("@SyncGuid") == syncGuid.ToString()),
            null,
            "text"), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoDataExists()
    {
        // Arrange
        var syncGuid = Guid.NewGuid();
        var webletGuid = Guid.NewGuid();
        var query = new GetDisciplinesBySyncGuidQuery(syncGuid, webletGuid);

        _cacheServiceMock.Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<IEnumerable<DisciplineCategoryDto>>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<string[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string key, Func<CancellationToken, Task<IEnumerable<DisciplineCategoryDto>>> factory, TimeSpan duration, string[] tags, CancellationToken token) => await factory(token));

        _disciplineCategoryReadRepositoryMock.Setup(r => r.GetListAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
            .ReturnsAsync(Enumerable.Empty<DisciplineCategoryDto>());


        // Act
        var result = await _disciplineHandler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldUseCorrectDisciplineSqlQuery_WhenCalled()
    {
        // Arrange
        var syncGuid = Guid.NewGuid();
        var webletGuid = Guid.NewGuid();
        var query = new GetDisciplinesBySyncGuidQuery(syncGuid, webletGuid);

        _cacheServiceMock.Setup(c => c.GetOrSetAsync(
                 It.IsAny<string>(),
                 It.IsAny<Func<CancellationToken, Task<IEnumerable<DisciplineCategoryDto>>>>(),
                 It.IsAny<TimeSpan>(),
                 It.IsAny<string[]>(),
                 It.IsAny<CancellationToken>()))
             .Returns(async (string key, Func<CancellationToken, Task<IEnumerable<DisciplineCategoryDto>>> factory, TimeSpan duration, string[] tags, CancellationToken token) => await factory(token));

        _disciplineCategoryReadRepositoryMock.Setup(r => r.GetListAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
            .ReturnsAsync(new List<DisciplineCategoryDto>());

        // Act
        await _disciplineHandler.Handle(query, CancellationToken.None);

        // Assert
        _disciplineCategoryReadRepositoryMock.Verify(r => r.GetListAsync(
            It.Is<string>(sql => GetDisciplinesSqlMatchesExpected(sql)),
            It.IsAny<CancellationToken>(),
            It.IsAny<DynamicParameters>(),
            null,
            "text"), Times.Once);
    }

    //[Theory]
    //[InlineData("")]
    //[InlineData("   ")]
    //[InlineData(null)]

    [Fact]
    public async Task Handle_ShouldReturnAgeGroupCategories_WhenDataExists()
    {
        // Arrange
        var syncGuid = Guid.NewGuid();
        var webletGuid = Guid.NewGuid();
        var query = new GetAgeGroupsBySyncGuidQuery(syncGuid, webletGuid);

        var mockData = new List<AgeGroupCategoryDto>
        {
            new()
            {
                Id = 1,
                Name = "Toddler (1-3 years)",
                MinAge = 1,
                MaxAge = 3
            },
            new()
            {
                Id = 2,
                Name = "Pre-schooled (3-5 years)",
                MinAge = 3,
                MaxAge = 5
            }
        };

        _cacheServiceMock.Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<IEnumerable<AgeGroupCategoryDto>>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<string[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string key, Func<CancellationToken, Task<IEnumerable<AgeGroupCategoryDto>>> factory, TimeSpan duration, string[] tags, CancellationToken token) => await factory(token));

        _ageGroupCategoryReadRepositoryMock.Setup(r => r.GetListAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
            .ReturnsAsync(mockData);

        // Act
        var result = await _ageGroupHandler.Handle(query, CancellationToken.None);

        // Assert
        var ageGroupCategoryDtos = result as AgeGroupCategoryDto[] ?? result.ToArray();
        ageGroupCategoryDtos.Should().NotBeNull();
        ageGroupCategoryDtos.Should().HaveCount(2);
        ageGroupCategoryDtos.First().Name.Should().Be("Toddler (1-3 years)");
        ageGroupCategoryDtos.Last().Name.Should().Be("Pre-schooled (3-5 years)");

        _ageGroupCategoryReadRepositoryMock.Verify(r => r.GetListAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            It.Is<DynamicParameters>(p => p.Get<string>("@SyncGuid") == syncGuid.ToString()),
            null,
            "text"), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoAgeGroupExists()
    {
        // Arrange
        var syncGuid = Guid.NewGuid();
        var webletGuid = Guid.NewGuid();
        var query = new GetAgeGroupsBySyncGuidQuery(syncGuid, webletGuid);

        _cacheServiceMock.Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<IEnumerable<AgeGroupCategoryDto>>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<string[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string key, Func<CancellationToken, Task<IEnumerable<AgeGroupCategoryDto>>> factory, TimeSpan duration, string[] tags, CancellationToken token) => await factory(token));

        _ageGroupCategoryReadRepositoryMock.Setup(r => r.GetListAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
            .ReturnsAsync(Enumerable.Empty<AgeGroupCategoryDto>());

        // Act
        var result = await _ageGroupHandler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldUseCorrectAgeGroupSqlQuery_WhenCalled()
    {
        // Arrange
        var syncGuid = Guid.NewGuid();
        var webletGuid = Guid.NewGuid();
        var query = new GetAgeGroupsBySyncGuidQuery(syncGuid, webletGuid);

        _cacheServiceMock.Setup(c => c.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<IEnumerable<AgeGroupCategoryDto>>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<string[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string key, Func<CancellationToken, Task<IEnumerable<AgeGroupCategoryDto>>> factory, TimeSpan duration, string[] tags, CancellationToken token) => await factory(token));

        _ageGroupCategoryReadRepositoryMock.Setup(r => r.GetListAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
            .ReturnsAsync(new List<AgeGroupCategoryDto>());

        // Act
        await _ageGroupHandler.Handle(query, CancellationToken.None);

        // Assert
        _ageGroupCategoryReadRepositoryMock.Verify(r => r.GetListAsync(
            It.Is<string>(sql => GetAgeGroupsSqlMatchesExpected(sql)),
            It.IsAny<CancellationToken>(),
            It.IsAny<DynamicParameters>(),
            null,
            "text"), Times.Once);
    }

    private static bool GetDisciplinesSqlMatchesExpected(string sql)
    {
        return sql.Contains("DECLARE @OwnerId INT") &&
               sql.Contains("Clubs_Default C") &&
               sql.Contains("INNER JOIN Document") &&
               sql.Contains("D.SyncGuid = @SyncGuid") &&
               sql.Contains("BC.OwnerId = @OwnerId") &&
               sql.Contains("JustGoBookingCategory") &&
               sql.Contains("JustGoBookingClassCategory") &&
               sql.Contains("JustGoBookingAttachment") &&
               sql.Contains("EntityTypeId = 2") &&
               sql.Contains("CC.CategoryType = 1");
    }

    private static bool GetAgeGroupsSqlMatchesExpected(string sql)
    {
        return sql.Contains("DECLARE @OwnerId INT") &&
               sql.Contains("Clubs_Default C") &&
               sql.Contains("INNER JOIN Document") &&
               sql.Contains("D.SyncGuid = @SyncGuid") &&
               sql.Contains("OwnerId = @OwnerId") &&
               sql.Contains("JustGoBookingAgeGroup") &&
               sql.Contains("JustGoBookingClassSession") &&
               sql.Contains("JustGoBookingAttachment") &&
               sql.Contains("EntityTypeId = 6") &&
               sql.Contains("IsActive = 1");
    }

    private static bool GetBasicClubDetailSqlMatchesExpected(string sql)
    {
        return sql.Contains("FROM Hierarchies H") &&
               sql.Contains("INNER JOIN Document D ON D.DocId = H.EntityId") &&
               sql.Contains("D.SyncGuid = @SyncGuid") &&
               sql.Contains("FROM EntitySetting WHERE EntityId = @EntityId");
    }

}
