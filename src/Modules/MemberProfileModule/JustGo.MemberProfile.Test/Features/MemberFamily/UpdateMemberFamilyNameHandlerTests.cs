using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.Features.MemberFamily.Commands.UpdateMemberFamilyName;
using JustGo.MemberProfile.Domain.Entities;
using JustGo.MemberProfile.Test.Helper;
using Moq;
using System.Data;

namespace JustGo.MemberProfile.Test.Features.MemberFamily
{
    public class UpdateMemberFamilyNameHandlerTests
    {
        private readonly Mock<IWriteRepositoryFactory> _writeRepositoryFactoryMock;
        private readonly Mock<IWriteRepository<Family_Default>> _repoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IDbTransaction> _transactionMock;
        private readonly UpdateMemberFamilyNameHandler _handler;

        public UpdateMemberFamilyNameHandlerTests()
        {
            _writeRepositoryFactoryMock = new Mock<IWriteRepositoryFactory>();
            _repoMock = new Mock<IWriteRepository<Family_Default>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _transactionMock = new Mock<IDbTransaction>();

            var lazyRepoMock = LazyServiceMockHelper.MockLazyService(_repoMock.Object);
            _writeRepositoryFactoryMock
                .Setup(x => x.GetLazyRepository<Family_Default>())
                .Returns(lazyRepoMock);

            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(_transactionMock.Object);
            _unitOfWorkMock.Setup(u => u.CommitAsync(It.IsAny<IDbTransaction>())).Returns(Task.CompletedTask);

            _handler = new UpdateMemberFamilyNameHandler(_writeRepositoryFactoryMock.Object, _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldUpdateFamilyName_WhenValidRequestIsProvided()
        {
            // Arrange
            var command = new UpdateMemberFamilyNameCommand
            {
                FamilySyncGuid = Guid.NewGuid(),
                FamilyName = "Updated Family Name"
            };

            _repoMock.Setup(r => r.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), It.IsAny<IDbTransaction>(), "Text"))
                     .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(1);

            _repoMock.Verify(r => r.ExecuteAsync(It.Is<string>(sql => sql.Contains("UPDATE [dbo].[Families]")),
                                                 It.IsAny<CancellationToken>(),
                                                 It.IsAny<object>(),
                                                 It.IsAny<IDbTransaction>(),
                                                 "Text"), Times.Once);

            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<IDbTransaction>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenUpdateFails()
        {
            // Arrange
            var command = new UpdateMemberFamilyNameCommand
            {
                FamilySyncGuid = Guid.NewGuid(),
                FamilyName = "Updated Family Name"
            };

            _repoMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), It.IsAny<IDbTransaction>(), "Text"))
                     .ThrowsAsync(new Exception("DB Error"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));

            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<IDbTransaction>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnZero_WhenNoRowsAffected()
        {
            // Arrange
            var command = new UpdateMemberFamilyNameCommand
            {
                FamilySyncGuid = Guid.NewGuid(),
                FamilyName = "Updated Family Name"
            };

            _repoMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), It.IsAny<IDbTransaction>(), "Text"))
                     .ReturnsAsync(0);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(0);

            _repoMock.Verify(r => r.ExecuteAsync(It.Is<string>(sql => sql.Contains("UPDATE [dbo].[Families]")),
                                                 It.IsAny<CancellationToken>(),
                                                 It.IsAny<object>(),
                                                 It.IsAny<IDbTransaction>(),
                                                 "Text"), Times.Once);

            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<IDbTransaction>()), Times.Once);
        }
    }
}
