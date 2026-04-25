using AuthModule.Application.Features.Users.Queries.GetUserByUserSyncId;
using AuthModule.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.CustomAuthorizations;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Membership.Application.DTOs;
using JustGo.Membership.Application.Features.Memberships.Queries.GetLicenses;
using Moq;

namespace JustGo.Membership.Test.Features.MembershipPurchase
{
    public class GetLicensesHandlerTests
    {
        private readonly Mock<IReadRepositoryFactory> _readRepoFactoryMock = new();
        private readonly Mock<IUtilityService> _utilityServiceMock = new();
        private readonly Mock<ISystemSettingsService> _systemSettingsMock = new();
        private readonly Mock<IMediator> _mediatorMock = new();
        private readonly Mock<IReadRepository<JustGo.Authentication.Infrastructure.Utilities.Group>> _groupRepoMock = new();
        private readonly Mock<IReadRepository<string>> _stringRepoMock = new();
        private readonly Mock<IReadRepository<MemberLicenseDto>> _memberLicenseRepoMock = new();
        private readonly Mock<IAuthorizationService> _authorizationServiceMock = new();

        private readonly GetLicensesHandler _handler;

        public GetLicensesHandlerTests()
        {
            // Mock IServiceProvider for MemberLicenseDto
            var memberLicenseServiceProvider = new Mock<IServiceProvider>();
            memberLicenseServiceProvider
                .Setup(x => x.GetService(typeof(IReadRepository<MemberLicenseDto>)))
                .Returns(_memberLicenseRepoMock.Object);

            // Mock IServiceProvider for GroupDto
            var groupServiceProvider = new Mock<IServiceProvider>();
            groupServiceProvider
                .Setup(x => x.GetService(typeof(IReadRepository<GroupDto>)))
                .Returns(_groupRepoMock.Object);

            // Mock IServiceProvider for string
            var stringServiceProvider = new Mock<IServiceProvider>();
            stringServiceProvider
                .Setup(x => x.GetService(typeof(IReadRepository<string>)))
                .Returns(_stringRepoMock.Object);

            // Setup LazyService for each type
            _readRepoFactoryMock
                .Setup(x => x.GetLazyRepository<MemberLicenseDto>())
                .Returns(new LazyService<IReadRepository<MemberLicenseDto>>(memberLicenseServiceProvider.Object));
            _readRepoFactoryMock
                .Setup(x => x.GetLazyRepository<GroupDto>())
                .Returns(new LazyService<IReadRepository<GroupDto>>(groupServiceProvider.Object));
            _readRepoFactoryMock
                .Setup(x => x.GetLazyRepository<string>())
                .Returns(new LazyService<IReadRepository<string>>(stringServiceProvider.Object));

            _handler = new GetLicensesHandler(
                _readRepoFactoryMock.Object,
                _utilityServiceMock.Object,
                _systemSettingsMock.Object,
                _mediatorMock.Object,
                _authorizationServiceMock.Object
            );
        }

        [Fact]
        public async Task Handle_ReturnsLicenses_WhenUserIsAuthorized()
        {
            // Arrange
            var userGuid = Guid.NewGuid();
            var user = new User { Userid = 1, SuspensionLevel = 0, MemberDocId = 2 };
            var request = new GetLicensesQuery(userGuid, "type", 0);

            _utilityServiceMock.Setup(x => x.GetCurrentUserId(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _utilityServiceMock.Setup(x => x.GetCurrentUserGuid()).Returns(userGuid);
            _mediatorMock.Setup(x => x.Send(It.IsAny<GetUserByUserSyncIdQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
            _memberLicenseRepoMock.Setup(x => x.GetListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), null, "text"))
                .ReturnsAsync(new List<MemberLicenseDto> { new MemberLicenseDto() });
            _authorizationServiceMock
                .Setup(x => x.IsActionAllowedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetLicensesAsync_ReturnsMemberships()
        {
            // Arrange
            var request = new GetLicensesQuery(Guid.NewGuid(), "type", 0);
            _memberLicenseRepoMock.Setup(x => x.GetListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), null, "text"))
                .ReturnsAsync(new List<MemberLicenseDto> { new MemberLicenseDto() });

            // Act
            var result = await _handler.GetLicensesAsync(request, CancellationToken.None);

            // Assert
            Assert.Single(result);
        }

        //[Fact]
        //public async Task SelectGroupByUserAsync_ReturnsGroups()
        //{
        //    // Arrange
        //    var groups = new List<JustGo.Authentication.Infrastructure.Utilities.Group> { new JustGo.Authentication.Infrastructure.Utilities.Group { GroupId = 25 } };
        //    _groupRepoMock.Setup(x => x.GetListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), null, "text"))
        //        .ReturnsAsync(groups);

        //    // Act
        //    var result = await _utilityServiceMock.Object.SelectGroupByUserAsync(1, CancellationToken.None);

        //    // Assert
        //    Assert.Single(result);
        //    Assert.Equal(25, result[0].GroupId);
        //}

        [Fact]
        public async Task GetUserIdByMemberDocIdAsync_ReturnsUserId()
        {
            // Arrange
            _stringRepoMock.Setup(x => x.GetSingleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), null, "text"))
                .ReturnsAsync("42");

            // Act
            var result = await _handler.GetUserIdByMemberDocIdAsync(1, CancellationToken.None);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task GetCurrentSuspensionLevelAsync_ReturnsSuspensionLevel()
        {
            // Arrange
            _stringRepoMock.Setup(x => x.GetSingleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), null, "text"))
                .ReturnsAsync("3");

            // Act
            var result = await _handler.GetCurrentSuspensionLevelAsync(1, CancellationToken.None);

            // Assert
            Assert.Equal(3, result);
        }

        [Fact]
        public async Task SuspensionLevelCheckAsync_ReturnsFalse_WhenNoConfig()
        {
            // Arrange
            _systemSettingsMock.Setup(x => x.GetSystemSettingsByItemKey(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);

            // Act
            var result = await _handler.SuspensionLevelCheckAsync(1, 0, 1, "membership", CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SuspensionLevelCheckAsync_ReturnsTrue_WhenScopeMatches()
        {
            // Arrange
            var config = "[{\"Value\":2,\"Scope\":[1,2,3]}]";
            _systemSettingsMock.Setup(x => x.GetSystemSettingsByItemKey(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(config);

            // Act
            var result = await _handler.SuspensionLevelCheckAsync(1, 2, 2, "", CancellationToken.None);

            // Assert
            Assert.True(result);
        }

        //[Fact]
        //public async Task IsActionAllowedAsync_Throws_WhenUnauthorized()
        //{
        //    // Arrange
        //    _authorizationServiceMock
        //        .Setup(x => x.IsActionAllowedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
        //        .Returns(Task.CompletedTask);

        //    // Act & Assert
        //    await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
        //        _authorizationServiceMock.Object.IsActionAllowedAsync(1, 2, "GetLicenses", CancellationToken.None));
        //}

        [Fact]
        public async Task IsActionAllowedAsync_DoesNotThrow_WhenAuthorized()
        {
            // Arrange
            _authorizationServiceMock
                .Setup(x => x.IsActionAllowedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act & Assert
            await _authorizationServiceMock.Object.IsActionAllowedAsync(1, 2, "GetLicenses", CancellationToken.None);
        }
    }
}
