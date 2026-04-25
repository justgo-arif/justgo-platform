using Dapper;
using FluentAssertions;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Organisation.Application.DTOs;
using JustGo.Organisation.Application.Features.Organizations.Queries.GetClubs;
using JustGo.Organisation.Test.Helper;
using JustGoAPI.Shared.Helper;
using Microsoft.AspNetCore.Http;
using Moq;
using Moq.Protected;
using System.Net;

namespace JustGo.Organisation.Test.Features.Organisations.Queries
{
    public class GetFilteredClubsHandlerTests
    {
        private readonly Mock<IReadRepositoryFactory> _readRepositoryFactoryMock;
        private readonly Mock<IReadRepository<ClubDto>> _readRepositoryMock;
        private readonly Mock<ISystemSettingsService> _systemSettingsMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly UserLocation _userLocation;
        private readonly GetClubsHandler _handler;

        public GetFilteredClubsHandlerTests()
        {
            _readRepositoryFactoryMock = new Mock<IReadRepositoryFactory>();
            _readRepositoryMock = new Mock<IReadRepository<ClubDto>>();
            _systemSettingsMock = new Mock<ISystemSettingsService>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();

            // Setup HttpClient mock to return default location
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"lat\":\"0\",\"lon\":\"0\"}")
                });

            var httpClient = new HttpClient(handlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Setup HttpContext to return empty IP
            var httpContext = new DefaultHttpContext();
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            _userLocation = new UserLocation(_httpContextAccessorMock.Object, _httpClientFactoryMock.Object);

            var lazyRepo = LazyServiceMockHelper.MockLazyService(_readRepositoryMock.Object);
            _readRepositoryFactoryMock
                .Setup(f => f.GetLazyRepository<ClubDto>())
                .Returns(lazyRepo);

            // Setup default system settings
            _systemSettingsMock
                .Setup(s => s.GetSystemSettingsByItemKey("CLUB.CLUBFINDERDEFAULTDISTANCEUNIT", It.IsAny<CancellationToken>()))
                .ReturnsAsync("km");

            _handler = new GetClubsHandler(
                _readRepositoryFactoryMock.Object,
                _systemSettingsMock.Object,
                _userLocation);
        }

        [Fact]
        public async Task Handle_ShouldReturnClubs_WhenResultsExist()
        {
            // Arrange
            var request = new GetClubsQuery
            {
                UserSyncId = Guid.NewGuid(),
                Regions = ["Brisbane"],
                ClubTypes = ["Gymnastics"],
                SortBy = "name",
                OrderBy = "asc",
                NumberOfRow = 10,
                Distance = 0
            };

            var clubsFromDb = new List<ClubDto>
            {
                new ClubDto { SyncGuid = Guid.NewGuid().ToString(), ClubName = "Club One", County = "Brisbane", RowNumber = 1, TotalRows = 2 },
                new ClubDto { SyncGuid = Guid.NewGuid().ToString(), ClubName = "Club Two", County = "Brisbane", RowNumber = 2, TotalRows = 2 }
            };

            _readRepositoryMock.Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    null,
                    "text"))
                .ReturnsAsync(clubsFromDb);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.HasMore.Should().BeFalse();
            result.Items.Should().BeEquivalentTo(clubsFromDb);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenNoClubsFound()
        {
            // Arrange
            var request = new GetClubsQuery
            {
                UserSyncId = Guid.NewGuid(),
                Regions = [],
                ClubTypes = [],
                SortBy = "name",
                OrderBy = "asc",
                NumberOfRow = 10,
                Distance = 0
            };

            _readRepositoryMock.Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    null,
                    "text"))
                .ReturnsAsync(new List<ClubDto>());

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.HasMore.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_ShouldBuildCorrectDynamicParameters()
        {
            // Arrange
            var userSyncId = Guid.NewGuid();
            var request = new GetClubsQuery
            {
                UserSyncId = userSyncId,
                Regions = ["Sydney", "Gold Coast"],
                ClubTypes = ["Martial Arts"],
                Distance = 15,
                KeySearch = "Elite",
                SortBy = "distance",
                OrderBy = "desc",
                NumberOfRow = 5,
                LastSeenId = 0
            };

            DynamicParameters capturedParameters = new DynamicParameters();

            _readRepositoryMock.Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    null,
                    "text"))
                .Callback<string, CancellationToken, object, object, string>((sql, token, parameters, transaction, type) =>
                {
                    capturedParameters = parameters as DynamicParameters ?? new DynamicParameters();
                })
                .ReturnsAsync(new List<ClubDto>());

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            capturedParameters.Should().NotBeNull();
            capturedParameters.Get<Guid>("UserSyncId").Should().Be(userSyncId);
            capturedParameters.Get<int>("LastSeenId").Should().Be(0);
            capturedParameters.Get<int>("NumberOfRows").Should().Be(6); // NumberOfRow + 1
            capturedParameters.Get<string>("Lat").Should().Be("0");
            capturedParameters.Get<string>("Lng").Should().Be("0");
            capturedParameters.Get<int>("IsKm").Should().Be(1);
            capturedParameters.Get<string>("Regions").Should().Be("Sydney,Gold Coast");
            capturedParameters.Get<string>("ClubTypes").Should().Be("Martial Arts");
        }

        [Fact]
        public async Task Handle_ShouldSetHasMoreTrue_WhenMoreRecordsExist()
        {
            // Arrange
            var request = new GetClubsQuery
            {
                UserSyncId = Guid.NewGuid(),
                Regions = [],
                ClubTypes = [],
                SortBy = "name",
                OrderBy = "asc",
                NumberOfRow = 2,
                Distance = 0
            };

            // Return 3 clubs when requesting 2 (NumberOfRow + 1 = 3)
            var clubsFromDb = new List<ClubDto>
            {
                new ClubDto { SyncGuid = Guid.NewGuid().ToString(), ClubName = "Club One", RowNumber = 1, TotalRows = 10 },
                new ClubDto { SyncGuid = Guid.NewGuid().ToString(), ClubName = "Club Two", RowNumber = 2, TotalRows = 10 },
                new ClubDto { SyncGuid = Guid.NewGuid().ToString(), ClubName = "Club Three", RowNumber = 3, TotalRows = 10 }
            };

            _readRepositoryMock.Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    null,
                    "text"))
                .ReturnsAsync(clubsFromDb);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2); // Last one removed
            result.HasMore.Should().BeTrue();
            result.TotalCount.Should().Be(10);
            result.LastSeenId.Should().Be(2);
        }

        [Fact]
        public async Task Handle_ShouldUseMilesWhenSystemSettingIsConfigured()
        {
            // Arrange
            _systemSettingsMock
                .Setup(s => s.GetSystemSettingsByItemKey("CLUB.CLUBFINDERDEFAULTDISTANCEUNIT", It.IsAny<CancellationToken>()))
                .ReturnsAsync("mile");

            var request = new GetClubsQuery
            {
                UserSyncId = Guid.NewGuid(),
                Regions = [],
                ClubTypes = [],
                SortBy = "name",
                OrderBy = "asc",
                NumberOfRow = 10,
                Distance = 0
            };

            DynamicParameters capturedParameters = new DynamicParameters();

            _readRepositoryMock.Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    null,
                    "text"))
                .Callback<string, CancellationToken, object, object, string>((sql, token, parameters, transaction, type) =>
                {
                    capturedParameters = parameters as DynamicParameters ?? new DynamicParameters();
                })
                .ReturnsAsync(new List<ClubDto>());

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            capturedParameters.Should().NotBeNull();
            capturedParameters.Get<int>("IsKm").Should().Be(0); // 0 for miles
        }

        [Fact]
        public async Task Handle_ShouldUseProvidedTotalRows_WhenTotalRowsIsSet()
        {
            // Arrange
            var request = new GetClubsQuery
            {
                UserSyncId = Guid.NewGuid(),
                Regions = [],
                ClubTypes = [],
                SortBy = "name",
                OrderBy = "asc",
                NumberOfRow = 10,
                Distance = 0,
                TotalRows = 100
            };

            var clubsFromDb = new List<ClubDto>
            {
                new ClubDto { SyncGuid = Guid.NewGuid().ToString(), ClubName = "Club One", RowNumber = 1, TotalRows = 50 }
            };

            _readRepositoryMock.Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    null,
                    "text"))
                .ReturnsAsync(clubsFromDb);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(100); // Uses provided TotalRows instead of DB value
        }
    }
}