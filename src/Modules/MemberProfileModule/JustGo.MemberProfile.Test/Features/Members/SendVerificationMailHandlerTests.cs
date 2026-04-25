using AuthModule.Application.Features.Users.Queries.GetUserByUserSyncId;
using AuthModule.Domain.Entities;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.Features.Members.Commands.SendVerificationMail;
using JustGo.MemberProfile.Test.Helper;
using Moq;
using System.Data;
using System.Data.Common;

namespace JustGo.MemberProfile.Test.Features.Members
{
    public class SendVerificationMailHandlerTests
    {
        private readonly Mock<IReadRepository<object>> _readRepositoryMock;
        private readonly Mock<IWriteRepository<object>> _writeRepositoryMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<ISystemSettingsService> _systemSettingsServiceMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IDbTransaction> _transactionMock;
        private readonly SendVerificationMailHandler _handler;

        public SendVerificationMailHandlerTests()
        {
            _readRepositoryMock = new Mock<IReadRepository<object>>();
            _writeRepositoryMock = new Mock<IWriteRepository<object>>();
            _mediatorMock = new Mock<IMediator>();
            _systemSettingsServiceMock = new Mock<ISystemSettingsService>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _transactionMock = new Mock<IDbTransaction>();

            // Setup UnitOfWork to return mocked transaction
            _unitOfWorkMock
                .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((DbTransaction)null!); // You may need to adjust this based on your actual implementation

            _unitOfWorkMock
                .Setup(u => u.BeginTransactionAsync())
                .ReturnsAsync(_transactionMock.Object);

            var lazyReadRepo = LazyServiceMockHelper.MockLazyService(_readRepositoryMock.Object);
            var lazyWriteRepo = LazyServiceMockHelper.MockLazyService(_writeRepositoryMock.Object);

            _handler = new SendVerificationMailHandler(
                lazyReadRepo,
                lazyWriteRepo,
                _mediatorMock.Object,
                _systemSettingsServiceMock.Object,
                _unitOfWorkMock.Object
            );
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenAllStepsSucceed()
        {
            // Arrange
            var userSyncId = Guid.NewGuid();
            var user = new User
            {
                Userid = 123,
                LoginId = "user123",
                EmailAddress = "user@example.com",
                SourceLocation = "Default"
            };

            var request = new SendVerificationMailCommand
            {
                UserSyncId = userSyncId,
                Type = "TwoFactor"
            };

            _mediatorMock
                .Setup(m => m.Send(It.Is<GetUserByUserSyncIdQuery>(q => q.UserSyncId == request.UserSyncId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _systemSettingsServiceMock
                .Setup(s => s.GetSystemSettingsByItemKey(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://example.com/");

            _readRepositoryMock
                .Setup(r => r.GetSingleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), It.IsAny<IDbTransaction>(), "text"))
                .ReturnsAsync(456);

            _writeRepositoryMock
                .Setup(r => r.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), It.IsAny<IDbTransaction>(), It.IsAny<string>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Verification email sent.");

            // Verify transaction was committed
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<IDbTransaction>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnError_WhenUserNotFound()
        {
            // Arrange
            var request = new SendVerificationMailCommand
            {
                UserSyncId = Guid.NewGuid(),
                Type = "TwoFactor"
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetUserByUserSyncIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Member not found.");

            // Verify no transaction operations were performed
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<IDbTransaction>()), Times.Never);
        }

    }
}