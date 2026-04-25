using Dapper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Organisation.Application.DTOs;
using JustGo.Organisation.Application.Features.Organizations.Queries.GetClubDetails;
using JustGo.Organisation.Test.Helper;
using Moq;

namespace JustGo.Organisation.Test.Features.Organisations.Queries
{
    public class GetClubDetailsHandlerTests
    {
        private readonly Mock<IReadRepositoryFactory> _readRepositoryFactoryMock;
        private readonly Mock<IReadRepository<ClubDetailsDto>> _repoMock;
        private readonly GetClubDetailsHandler _handler;

        public GetClubDetailsHandlerTests()
        {
            _readRepositoryFactoryMock = new Mock<IReadRepositoryFactory>();
            _repoMock = new Mock<IReadRepository<ClubDetailsDto>>();

            var lazyRepo = LazyServiceMockHelper.MockLazyService(_repoMock.Object);
            _readRepositoryFactoryMock
                .Setup(f => f.GetLazyRepository<ClubDetailsDto>())
                .Returns(lazyRepo);

            _handler = new GetClubDetailsHandler(_readRepositoryFactoryMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnClubDetails_WhenClubExists()
        {
            // Arrange
            var clubGuid = Guid.NewGuid();
            var userGuid = Guid.NewGuid();
            var request = new GetClubDetailsQuery(clubGuid, userGuid);

            var expectedClub = new ClubDetailsDto
            {
                ClubDocId = 123,
                SyncGuid = clubGuid.ToString(),
                ClubName = "Sample Club",
                ClubId = "CLUB001",
                EmailAddress = "sample@club.com",
                Lat = "23.77",
                Lng = "90.39"
            };

            _repoMock.Setup(r => r.GetAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    null,
                    "text"))
                .ReturnsAsync(expectedClub);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedClub);
        }

        [Fact]
        public async Task Handle_ShouldThrowKeyNotFoundException_WhenClubDoesNotExist()
        {
            // Arrange
            var clubGuid = Guid.NewGuid();
            var userGuid = Guid.NewGuid();
            var request = new GetClubDetailsQuery(clubGuid, userGuid);

            _repoMock.Setup(r => r.GetAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    null,
                    "text"))
                .ReturnsAsync((ClubDetailsDto?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(request, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ShouldExecuteCorrectSql_WithClubSyncIdAndUserSyncIdParameters()
        {
            // Arrange
            var clubGuid = Guid.NewGuid();
            var userGuid = Guid.NewGuid();
            var request = new GetClubDetailsQuery(clubGuid, userGuid);

            string? executedSql = null;
            DynamicParameters? capturedParams = null;

            _repoMock.Setup(r => r.GetAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    null,
                    "text"))
                .Callback<string, CancellationToken, object, object, string>((sql, _, param, _, _) =>
                {
                    executedSql = sql;
                    capturedParams = param as DynamicParameters;
                })
                .ReturnsAsync(new ClubDetailsDto
                {
                    ClubDocId = 123,
                    SyncGuid = clubGuid.ToString(),
                    ClubName = "Test Club",
                    ClubId = "CLUB001"
                });

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            executedSql.Should().NotBeNull();
            executedSql.Should().Contain("FROM Clubs_Default");
            executedSql.Should().Contain("WHERE D.SyncGuid = @ClubSyncId");
            executedSql.Should().Contain("WHERE U.UserSyncId = @UserSyncId");

            capturedParams.Should().NotBeNull();
            capturedParams!.Get<Guid>("ClubSyncId").Should().Be(clubGuid);
            capturedParams.Get<Guid>("UserSyncId").Should().Be(userGuid);
        }

        [Fact]
        public async Task Handle_ShouldIncludeJoinedStatus_WhenUserIsJoined()
        {
            // Arrange
            var clubGuid = Guid.NewGuid();
            var userGuid = Guid.NewGuid();
            var request = new GetClubDetailsQuery(clubGuid, userGuid);

            var expectedClub = new ClubDetailsDto
            {
                ClubDocId = 123,
                SyncGuid = clubGuid.ToString(),
                ClubName = "Sample Club",
                ClubId = "CLUB001",
                IsJoined = true,
                JoinedDate = DateTime.UtcNow.AddDays(-30)
            };

            _repoMock.Setup(r => r.GetAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    null,
                    "text"))
                .ReturnsAsync(expectedClub);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.IsJoined.Should().BeTrue();
            result.JoinedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_ShouldIncludeMemberStatusAndRoles_WhenUserHasRoles()
        {
            // Arrange
            var clubGuid = Guid.NewGuid();
            var userGuid = Guid.NewGuid();
            var request = new GetClubDetailsQuery(clubGuid, userGuid);

            var expectedClub = new ClubDetailsDto
            {
                ClubDocId = 123,
                SyncGuid = clubGuid.ToString(),
                ClubName = "Sample Club",
                ClubId = "CLUB001",
                MemberStatus = "Active",
                MemberRoles = "Coach,Admin"
            };

            _repoMock.Setup(r => r.GetAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    null,
                    "text"))
                .ReturnsAsync(expectedClub);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.MemberStatus.Should().Be("Active");
            result.MemberRoles.Should().Be("Coach,Admin");
            result.Roles.Should().BeEquivalentTo(new[] { "Coach", "Admin" });
        }

        [Fact]
        public async Task Handle_ShouldIncludeLocationData_WhenAvailable()
        {
            // Arrange
            var clubGuid = Guid.NewGuid();
            var userGuid = Guid.NewGuid();
            var request = new GetClubDetailsQuery(clubGuid, userGuid);

            var expectedClub = new ClubDetailsDto
            {
                ClubDocId = 123,
                SyncGuid = clubGuid.ToString(),
                ClubName = "Sample Club",
                ClubId = "CLUB001",
                Address1 = "123 Main St",
                Town = "London",
                Postcode = "SW1A 1AA",
                County = "Greater London",
                Country = "UK",
                Lat = "51.5074",
                Lng = "-0.1278",
                Distance = 5.5m
            };

            _repoMock.Setup(r => r.GetAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    null,
                    "text"))
                .ReturnsAsync(expectedClub);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result!.Address1.Should().Be("123 Main St");
            result.Town.Should().Be("London");
            result.Lat.Should().Be("51.5074");
            result.Lng.Should().Be("-0.1278");
            result.Distance.Should().Be(5.5m);
        }

    }
}