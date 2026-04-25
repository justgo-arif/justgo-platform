using System.Data;
using Dapper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Organisation.Application.Features.Organizations.Commands.LeaveClub;
using JustGo.Organisation.Test.Helper;
using JustGo.MemberProfile.Application.DTOs;
using Moq;

namespace JustGo.Organisation.Test.Features.Organisations.Commands
{
    public class LeaveClubHandlerTests
    {
        private readonly Mock<IWriteRepository<object>> _writeRepositoryMock;
        private readonly Mock<IWriteRepositoryFactory> _writeRepositoryFactoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IUtilityService> _utilityServiceMock;
        private readonly Mock<IDbTransaction> _transactionMock;
        private readonly LeaveClubHandler _handler;

        public LeaveClubHandlerTests()
        {
            _writeRepositoryMock = new Mock<IWriteRepository<object>>();
            _writeRepositoryFactoryMock = new Mock<IWriteRepositoryFactory>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _utilityServiceMock = new Mock<IUtilityService>();
            _transactionMock = new Mock<IDbTransaction>();

            var lazyRepo = LazyServiceMockHelper.MockLazyService(_writeRepositoryMock.Object);

            _writeRepositoryFactoryMock
                .Setup(f => f.GetLazyRepository<object>())
                .Returns(lazyRepo);

            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync())
                .ReturnsAsync(_transactionMock.Object);
            
            _unitOfWorkMock.Setup(u => u.CommitAsync(It.IsAny<IDbTransaction>()))
                .Returns(Task.CompletedTask);

            _utilityServiceMock.Setup(u => u.GetCurrentUserId(It.IsAny<CancellationToken>()))
                .ReturnsAsync(123);

            _handler = new LeaveClubHandler(
                _writeRepositoryFactoryMock.Object,
                _unitOfWorkMock.Object,
                _utilityServiceMock.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnSuccess_WhenMemberLeavesClubSuccessfully()
        {
            // Arrange
            var command = new LeaveClubCommand
            {
                ClubGuid = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                MemberGuid = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Reason = "Personal reasons",
                ClubMemberRoles = "Member"
            };

            DynamicParameters? capturedParams = null;

            _writeRepositoryMock.Setup(r => r.ExecuteAsync(
                    "RejectClubMemberBySyncGuid",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object>(),
                    It.IsAny<IDbTransaction>(),
                    "sp"))
                .Callback<string, CancellationToken, object, IDbTransaction, string>((_, _, param, _, _) =>
                {
                    capturedParams = param as DynamicParameters;
                })
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Club member removed successfully.");
            result.RowsAffected.Should().Be(1);

            capturedParams.Should().NotBeNull();
            capturedParams.Get<Guid>("@ClubSyncGuid").Should().Be(command.ClubGuid);
            capturedParams.Get<Guid>("@MemberSyncGuid").Should().Be(command.MemberGuid);
            capturedParams.Get<string>("@Reason").Should().Be(command.Reason);
            capturedParams.Get<int>("@ActionUserId").Should().Be(123);

            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(_transactionMock.Object), Times.Once);
            _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<IDbTransaction>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_WhenExceptionOccurs()
        {
            // Arrange
            var command = new LeaveClubCommand
            {
                ClubGuid = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                MemberGuid = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Reason = "Test reason",
                ClubMemberRoles = "Member"
            };

            var exceptionMessage = "Database error occurred";

            _writeRepositoryMock.Setup(r => r.ExecuteAsync(
                    "RejectClubMemberBySyncGuid",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object>(),
                    It.IsAny<IDbTransaction>(),
                    "sp"))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be(exceptionMessage);
            result.RowsAffected.Should().Be(0);

            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<IDbTransaction>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.RollbackAsync(_transactionMock.Object), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_UseCurrentUserId_FromUtilityService()
        {
            // Arrange
            var expectedUserId = 456;
            _utilityServiceMock.Setup(u => u.GetCurrentUserId(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedUserId);

            var command = new LeaveClubCommand
            {
                ClubGuid = Guid.NewGuid(),
                MemberGuid = Guid.NewGuid(),
                Reason = "Test reason",
                ClubMemberRoles = "Member"
            };

            DynamicParameters? capturedParams = null;

            _writeRepositoryMock.Setup(r => r.ExecuteAsync(
                    "RejectClubMemberBySyncGuid",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object>(),
                    It.IsAny<IDbTransaction>(),
                    "sp"))
                .Callback<string, CancellationToken, object, IDbTransaction, string>((_, _, param, _, _) =>
                {
                    capturedParams = param as DynamicParameters;
                })
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            capturedParams.Should().NotBeNull();
            capturedParams.Get<int>("@ActionUserId").Should().Be(expectedUserId);

            _utilityServiceMock.Verify(u => u.GetCurrentUserId(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_CallRejectClubMemberBySyncGuid_WithCorrectParameters()
        {
            // Arrange
            var clubGuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var memberGuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var reason = "No longer interested";
            var currentUserId = 789;

            _utilityServiceMock.Setup(u => u.GetCurrentUserId(It.IsAny<CancellationToken>()))
                .ReturnsAsync(currentUserId);

            var command = new LeaveClubCommand
            {
                ClubGuid = clubGuid,
                MemberGuid = memberGuid,
                Reason = reason,
                ClubMemberRoles = "Admin"
            };

            DynamicParameters? capturedParams = null;
            IDbTransaction? capturedTransaction = null;

            _writeRepositoryMock.Setup(r => r.ExecuteAsync(
                    "RejectClubMemberBySyncGuid",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object>(),
                    It.IsAny<IDbTransaction>(),
                    "sp"))
                .Callback<string, CancellationToken, object, IDbTransaction, string>((_, _, param, trans, _) =>
                {
                    capturedParams = param as DynamicParameters;
                    capturedTransaction = trans;
                })
                .ReturnsAsync(1);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            capturedParams.Should().NotBeNull();
            capturedParams.Get<Guid>("@ClubSyncGuid").Should().Be(clubGuid);
            capturedParams.Get<Guid>("@MemberSyncGuid").Should().Be(memberGuid);
            capturedParams.Get<string>("@Reason").Should().Be(reason);
            capturedParams.Get<int>("@ActionUserId").Should().Be(currentUserId);
            capturedTransaction.Should().Be(_transactionMock.Object);

            _writeRepositoryMock.Verify(r => r.ExecuteAsync(
                "RejectClubMemberBySyncGuid",
                It.IsAny<CancellationToken>(),
                It.Is<DynamicParameters>(p => 
                    p.Get<Guid>("@ClubSyncGuid") == clubGuid &&
                    p.Get<Guid>("@MemberSyncGuid") == memberGuid &&
                    p.Get<string>("@Reason") == reason &&
                    p.Get<int>("@ActionUserId") == currentUserId),
                _transactionMock.Object,
                "sp"), Times.Once);
        }
    }
}
