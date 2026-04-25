using System.Data;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Commands.SetPrimaryUserEmergencyContact;
using JustGo.MemberProfile.Domain.Entities;
using JustGo.MemberProfile.Test.Helper;
using Moq;
namespace JustGo.MemberProfile.Test.Features.UserEmergencyContacts.Commands
{
    public class SetPrimaryUserEmergencyContactHandlerTests
    {
        private readonly Mock<IWriteRepositoryFactory> _writeRepositoryFactoryMock = new();
        private readonly Mock<IWriteRepository<UserEmergencyContact>> _repoMock = new();
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IUtilityService> _utilityServiceMock = new();
        private readonly Mock<IDbTransaction> _transactionMock = new();

        private readonly SetPrimaryUserEmergencyContactHandler _handler;

        public SetPrimaryUserEmergencyContactHandlerTests()
        {
            var lazyRepo = LazyServiceMockHelper.MockLazyService(_repoMock.Object);

            _writeRepositoryFactoryMock
                .Setup(f => f.GetLazyRepository<UserEmergencyContact>())
                .Returns(lazyRepo);

            _unitOfWorkMock
                .Setup(u => u.BeginTransactionAsync())
                .ReturnsAsync(_transactionMock.Object);

            _handler = new SetPrimaryUserEmergencyContactHandler(
                _writeRepositoryFactoryMock.Object,
                _unitOfWorkMock.Object,
                _utilityServiceMock.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldUnsetAndSetPrimaryContact_AndCommitTransaction()
        {
            // Arrange
            var command = new SetPrimaryUserEmergencyContactCommand(2);

            _utilityServiceMock
                .Setup(u => u.GetCurrentUserId(It.IsAny<CancellationToken>()))
                .ReturnsAsync(14513);

            _repoMock
                .Setup(r => r.ExecuteAsync(
                    It.Is<string>(s => s.Contains("SET IsPrimary = 0")),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object>(),
                    _transactionMock.Object,
                    "Text"))
                .ReturnsAsync(1);

            _repoMock
                .Setup(r => r.ExecuteAsync(
                    It.Is<string>(s => s.Contains("SET IsPrimary = 1")),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object>(),
                    _transactionMock.Object,
                    "Text"))
                .ReturnsAsync(1);

            _unitOfWorkMock
                .Setup(u => u.CommitAsync(_transactionMock.Object))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(1);

            _repoMock.Verify(r => r.ExecuteAsync(
                It.Is<string>(s => s.Contains("SET IsPrimary = 0")),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                _transactionMock.Object,
                "Text"), Times.Once);

            _repoMock.Verify(r => r.ExecuteAsync(
                It.Is<string>(s => s.Contains("SET IsPrimary = 1")),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                _transactionMock.Object,
                "Text"), Times.Once);

            _unitOfWorkMock.Verify(u => u.CommitAsync(_transactionMock.Object), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnZero_WhenUserIdIsZero()
        {
            // Arrange
            var command = new SetPrimaryUserEmergencyContactCommand(2);

            _utilityServiceMock
                .Setup(u => u.GetCurrentUserId(It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(0);

            _repoMock.Verify(r => r.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                It.IsAny<string>()), Times.Never);

            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<IDbTransaction>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenExecuteFails()
        {
            // Arrange
            var command = new SetPrimaryUserEmergencyContactCommand(2);

            _utilityServiceMock
                .Setup(u => u.GetCurrentUserId(It.IsAny<CancellationToken>()))
                .ReturnsAsync(14513);

            _repoMock
                .Setup(r => r.ExecuteAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object>(),
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new Exception("DB error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));

            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<IDbTransaction>()), Times.Never);
        }
    }
}
