using Dapper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Membership.Application.DTOs;
using JustGo.Membership.Application.Features.Memberships.Queries.GetMyClubsBySyncGuid;
using JustGo.Membership.Test.Helper;
using Moq;

namespace JustGo.Membership.Test.Features.MembershipPurchase
{
    public class GetMyClubsBySyncGuidHandlerTests
    {
        private readonly Mock<IReadRepository<ClubInfoDto>> _readRepositoryMock;
        private readonly Mock<IReadRepository<LicenseDto>> _licenseRepositoryMock;
        private readonly GetMyClubsBySyncGuidHandler _handler;

        public GetMyClubsBySyncGuidHandlerTests()
        {
            _readRepositoryMock = new Mock<IReadRepository<ClubInfoDto>>();
            _licenseRepositoryMock = new Mock<IReadRepository<LicenseDto>>();

            var lazyReadRepository = LazyServiceMockHelper.MockLazyService(_readRepositoryMock.Object);
            var lazyLicenseRepository = LazyServiceMockHelper.MockLazyService(_licenseRepositoryMock.Object);

            _handler = new GetMyClubsBySyncGuidHandler(lazyReadRepository, lazyLicenseRepository);
        }

        [Fact]
        public async Task Handle_ShouldReturnClubsWithLicenses()
        {
            // Arrange
            var userSyncId = Guid.NewGuid();

            var clubs = new List<ClubInfoDto>
        {
            new ClubInfoDto { ClubDocId = 1, ClubName = "Club A" },
            new ClubInfoDto { ClubDocId = 2, ClubName = "Club B" }
        };

            var licenses = new List<LicenseDto>
        {
            new LicenseDto { ClubDocId = 1, LicenseDocId = 101, Name = "License 1" },
            new LicenseDto { ClubDocId = 2, LicenseDocId = 102, Name = "License 2" },
            new LicenseDto { ClubDocId = 1, LicenseDocId = 103, Name = "License 3" }
        };

            _readRepositoryMock
                .Setup(r => r.GetListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<DynamicParameters>(), null, "text"))
                .ReturnsAsync(clubs);

            _licenseRepositoryMock
                .Setup(r => r.GetListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<DynamicParameters>(), null, "text"))
                .ReturnsAsync(licenses);

            var query = new GetMyClubsBySyncGuidQuery(Guid.NewGuid());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count);

            var clubA = result.First(c => c.ClubDocId == 1);
            Assert.Equal(2, clubA.Licenses.Count); // License 1 and License 3

            var clubB = result.First(c => c.ClubDocId == 2);
            Assert.Single(clubB.Licenses); // License 2

            _readRepositoryMock.Verify(r => r.GetListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<DynamicParameters>(), null, "text"), Times.Once);
            _licenseRepositoryMock.Verify(r => r.GetListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<DynamicParameters>(), null, "text"), Times.Once);
        }
    }
}
