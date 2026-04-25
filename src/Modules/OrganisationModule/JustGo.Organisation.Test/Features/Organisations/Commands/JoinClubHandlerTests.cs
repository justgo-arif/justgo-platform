using Dapper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Organisation.Application.Features.Organizations.Commands.AddClub;
using JustGo.Organisation.Test.Helper;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Data;
using System.Security.Claims;

namespace JustGo.Organisation.Test.Features.Organisations.Commands;

public class JoinClubHandlerTests
{
    private readonly Mock<IWriteRepository<object>> _writeRepositoryMock;
    private readonly Mock<IWriteRepositoryFactory> _writeRepositoryFactoryMock;
    private readonly Mock<IReadRepository<object>> _readRepositoryMock;
    private readonly Mock<IReadRepositoryFactory> _readRepositoryFactoryMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IUtilityService> _utilityServiceMock;
    private readonly JoinClubHandler _handler;

    public JoinClubHandlerTests()
    {
        _writeRepositoryMock = new Mock<IWriteRepository<object>>();
        _writeRepositoryFactoryMock = new Mock<IWriteRepositoryFactory>();
        _readRepositoryMock = new Mock<IReadRepository<object>>();
        _readRepositoryFactoryMock = new Mock<IReadRepositoryFactory>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mediatorMock = new Mock<IMediator>();
        _utilityServiceMock = new Mock<IUtilityService>();

        // Lazy write repository setup
        var lazyWriteRepo = LazyServiceMockHelper.MockLazyService(_writeRepositoryMock.Object);
        _writeRepositoryFactoryMock
            .Setup(f => f.GetLazyRepository<object>())
            .Returns(lazyWriteRepo);

        // Lazy read repository setup
        var lazyReadRepo = LazyServiceMockHelper.MockLazyService(_readRepositoryMock.Object);
        _readRepositoryFactoryMock
            .Setup(f => f.GetLazyRepository<object>())
            .Returns(lazyReadRepo);

        // Default HttpContext with UserSyncId claim
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim("UserSyncId", Guid.NewGuid().ToString())
        }));
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        // Unit‑of‑Work stubs
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync())
            .ReturnsAsync(Mock.Of<IDbTransaction>());
        _unitOfWorkMock.Setup(u => u.CommitAsync(It.IsAny<IDbTransaction>()))
            .Returns(Task.CompletedTask);

        // Mediator stub
        _mediatorMock.Setup(m => m.Send(It.IsAny<IRequest<object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());

        // UtilityService default behaviour
        _utilityServiceMock.Setup(u => u.GetCurrentUserId(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(123);

        _handler = new JoinClubHandler(
            _readRepositoryFactoryMock.Object,
            _writeRepositoryFactoryMock.Object,
            _httpContextAccessorMock.Object,
            _unitOfWorkMock.Object,
            _mediatorMock.Object,
            _utilityServiceMock.Object);
    }

    private void SetupGetSingleAsyncWithOutputParams()
    {
        _readRepositoryMock
            .Setup(r => r.GetSingleAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                It.IsAny<string>()))
            .Returns<string, CancellationToken, object, IDbTransaction, string>((proc, token, paramObj, tx, cmdType) =>
            {
                var param = paramObj as DynamicParameters;
                if (param != null)
                {
                    var parametersField = param.GetType().GetField("parameters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var parameters = parametersField?.GetValue(param) as System.Collections.IDictionary;

                    if (parameters != null)
                    {
                        if (parameters.Contains("ClubDocId"))
                        {
                            var clubDocIdParam = parameters["ClubDocId"];
                            clubDocIdParam?.GetType().GetProperty("Value")?.SetValue(clubDocIdParam, 10);
                        }
                        if (parameters.Contains("MemberDocId"))
                        {
                            var memberDocIdParam = parameters["MemberDocId"];
                            memberDocIdParam?.GetType().GetProperty("Value")?.SetValue(memberDocIdParam, 20);
                        }
                    }
                }
                return Task.FromResult<object?>(new object());
            });
    }

    [Fact]
    public async Task Handle_Should_SaveAndLinkClubMember_WhenInputIsValid()
    {
        // Arrange
        var clubGuid = Guid.NewGuid();
        var memberGuid = Guid.NewGuid();

        var command = new JoinClubCommand
        {
            ClubGuid = clubGuid,
            MemberGuid = memberGuid,
            ClubMemberRoles = "Admin"
        };

        // Setup GetSingleAsync with output parameters simulation
        SetupGetSingleAsyncWithOutputParams();

        // Mock ExecuteAsync (called by JoinClubAsync)
        _writeRepositoryMock.Setup(r => r.ExecuteAsync(
                It.Is<string>(s => s == "SaveAndLinkClubMember"),
                It.IsAny<CancellationToken>(),
                It.IsAny<DynamicParameters>(),
                It.IsAny<IDbTransaction>(),
                It.IsAny<string>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("Member joined successfully.");

        // Verify GetSingleAsync was called
        _readRepositoryMock.Verify(r => r.GetSingleAsync(
            It.Is<string>(s => s == "GetMemberClubDocIdBySyncGuid"),
            It.IsAny<CancellationToken>(),
            It.IsAny<object>(),
            It.IsAny<IDbTransaction>(),
            It.IsAny<string>()), Times.Once);

        // Verify ExecuteAsync was called
        _writeRepositoryMock.Verify(r => r.ExecuteAsync(
            It.Is<string>(s => s == "SaveAndLinkClubMember"),
            It.IsAny<CancellationToken>(),
            It.IsAny<DynamicParameters>(),
            It.IsAny<IDbTransaction>(),
            It.IsAny<string>()), Times.Once);

        // Verify transaction was committed
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<IDbTransaction>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowUnauthorizedAccessException_WhenUserSyncIdMissing()
    {
        // Arrange
        _utilityServiceMock.Setup(u => u.GetCurrentUserId(It.IsAny<CancellationToken>()))
                           .ThrowsAsync(new UnauthorizedAccessException("User Sync ID not found."));

        var command = new JoinClubCommand
        {
            ClubGuid = Guid.NewGuid(),
            MemberGuid = Guid.NewGuid()
        };

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
                 .WithMessage("User Sync ID not found.");
    }

    [Fact]
    public async Task Handle_Should_SetAdminGroupId_WhenClubMemberRolesContainsAdmin()
    {
        // Arrange
        var clubGuid = Guid.NewGuid();
        var memberGuid = Guid.NewGuid();

        var command = new JoinClubCommand
        {
            ClubGuid = clubGuid,
            MemberGuid = memberGuid,
            ClubMemberRoles = "Admin"
        };

        DynamicParameters? capturedParams = null;

        // Setup GetSingleAsync
        SetupGetSingleAsyncWithOutputParams();

        _writeRepositoryMock.Setup(r => r.ExecuteAsync(
                It.Is<string>(s => s == "SaveAndLinkClubMember"),
                It.IsAny<CancellationToken>(),
                It.IsAny<DynamicParameters>(),
                It.IsAny<IDbTransaction>(),
                It.IsAny<string>()))
            .Callback<string, CancellationToken, object, IDbTransaction, string>((proc, token, paramObj, tx, cmdType) =>
            {
                capturedParams = paramObj as DynamicParameters;
            })
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        capturedParams.Should().NotBeNull();
        capturedParams!.Get<string>("@UserGroupIds").Should().Be("24,26"); // Admin group includes 26
    }

    [Fact]
    public async Task Handle_Should_SetMemberGroupId_WhenClubMemberRolesDoesNotContainAdmin()
    {
        // Arrange
        var clubGuid = Guid.NewGuid();
        var memberGuid = Guid.NewGuid();

        var command = new JoinClubCommand
        {
            ClubGuid = clubGuid,
            MemberGuid = memberGuid,
            ClubMemberRoles = "Member"
        };

        DynamicParameters? capturedParams = null;

        // Setup GetSingleAsync
        SetupGetSingleAsyncWithOutputParams();

        _writeRepositoryMock.Setup(r => r.ExecuteAsync(
                It.Is<string>(s => s == "SaveAndLinkClubMember"),
                It.IsAny<CancellationToken>(),
                It.IsAny<DynamicParameters>(),
                It.IsAny<IDbTransaction>(),
                It.IsAny<string>()))
            .Callback<string, CancellationToken, object, IDbTransaction, string>((proc, token, paramObj, tx, cmdType) =>
            {
                capturedParams = paramObj as DynamicParameters;
            })
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        capturedParams.Should().NotBeNull();
        capturedParams!.Get<string>("@UserGroupIds").Should().Be("24"); // Regular member group
    }

    [Fact]
    public async Task Handle_Should_PassCorrectParametersToSaveAndLinkClubMember()
    {
        // Arrange
        var clubGuid = Guid.NewGuid();
        var memberGuid = Guid.NewGuid();
        var userId = 123;

        var command = new JoinClubCommand
        {
            ClubGuid = clubGuid,
            MemberGuid = memberGuid,
            ClubMemberRoles = "Coach"
        };

        DynamicParameters? capturedParams = null;

        _utilityServiceMock.Setup(u => u.GetCurrentUserId(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(userId);

        // Setup GetSingleAsync
        SetupGetSingleAsyncWithOutputParams();

        _writeRepositoryMock.Setup(r => r.ExecuteAsync(
                It.Is<string>(s => s == "SaveAndLinkClubMember"),
                It.IsAny<CancellationToken>(),
                It.IsAny<DynamicParameters>(),
                It.IsAny<IDbTransaction>(),
                It.IsAny<string>()))
            .Callback<string, CancellationToken, object, IDbTransaction, string>((proc, token, paramObj, tx, cmdType) =>
            {
                capturedParams = paramObj as DynamicParameters;
            })
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        capturedParams.Should().NotBeNull();
        capturedParams!.Get<int>("@ClubDocId").Should().Be(10);
        capturedParams!.Get<int>("@MemberDocId").Should().Be(20);
        capturedParams.Get<string>("@UserRoleIds").Should().Be("10,13,27");
        capturedParams.Get<string>("@ClubMemberRoles").Should().Be("Coach");
        capturedParams.Get<string>("@ClubMembershipCategory").Should().Be("Member");
        capturedParams.Get<int>("@UserId").Should().Be(userId);
        capturedParams.Get<DateTime>("@ClubMembershipExpiry").Should().Be(new DateTime(2099, 12, 31, 0, 0, 0, DateTimeKind.Utc));
    }
}