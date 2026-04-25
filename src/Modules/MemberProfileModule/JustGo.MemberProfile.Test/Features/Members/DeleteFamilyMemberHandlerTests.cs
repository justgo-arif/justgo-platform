using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.Features.Members.Commands.DeleteFamilyMember;
using JustGo.MemberProfile.Test.Helper;
using Moq;
using System.Data;

namespace JustGo.MemberProfile.Test.Features.Members
{
    public class DeleteFamilyMemberHandlerTests
    {
        private readonly Mock<IWriteRepositoryFactory> _writeRepositoryFactoryMock;
        private readonly Mock<IWriteRepository<object>> _repoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IDbTransaction> _transactionMock;
        private readonly DeleteFamilyMemberHandler _handler;

        public DeleteFamilyMemberHandlerTests()
        {
            _writeRepositoryFactoryMock = new Mock<IWriteRepositoryFactory>();
            _repoMock = new Mock<IWriteRepository<object>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _transactionMock = new Mock<IDbTransaction>();

            var lazyRepoMock = LazyServiceMockHelper.MockLazyService(_repoMock.Object);
            _writeRepositoryFactoryMock
                .Setup(x => x.GetLazyRepository<object>())
                .Returns(lazyRepoMock);

            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(_transactionMock.Object);
            _unitOfWorkMock.Setup(u => u.CommitAsync(It.IsAny<IDbTransaction>())).Returns(Task.CompletedTask);

            _handler = new DeleteFamilyMemberHandler(_writeRepositoryFactoryMock.Object, _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldDeleteFamilyMember_WhenValidRequestIsProvided()
        {
            // Arrange
            var command = new DeleteFamilyMemberCommand(familyDocId: 1, memberDocId: 2);

            _repoMock.Setup(r => r.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), It.IsAny<IDbTransaction>(), "text"))
                     .ReturnsAsync(2); // Assume 2 rows were affected (successful deletion)

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(2); // Assert that 2 rows were deleted

            // Verify that the correct SQL was executed
            _repoMock.Verify(r => r.ExecuteAsync(It.Is<string>(sql => sql.Contains("DELETE FROM [dbo].[Family_Links]") && sql.Contains("DELETE FROM [dbo].[Members_Links]")),
                                                 It.IsAny<CancellationToken>(),
                                                 It.IsAny<object>(),
                                                 It.IsAny<IDbTransaction>(),
                                                 "text"), Times.Once);

            // Verify that CommitAsync was called once to commit the transaction
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<IDbTransaction>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnZero_WhenNoRowsAffected()
        {
            // Arrange
            var command = new DeleteFamilyMemberCommand(familyDocId: 1, memberDocId: 2);

            _repoMock.Setup(r => r.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), It.IsAny<IDbTransaction>(), "text"))
                     .ReturnsAsync(0); // No rows affected

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(0); // Assert that no rows were deleted

            // Verify that the correct SQL was executed
            _repoMock.Verify(r => r.ExecuteAsync(It.Is<string>(sql => sql.Contains("DELETE FROM [dbo].[Family_Links]") && sql.Contains("DELETE FROM [dbo].[Members_Links]")),
                                                 It.IsAny<CancellationToken>(),
                                                 It.IsAny<object>(),
                                                 It.IsAny<IDbTransaction>(),
                                                 "text"), Times.Once);

            // Verify that CommitAsync was called once to commit the transaction
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<IDbTransaction>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenDeleteFails()
        {
            // Arrange
            var command = new DeleteFamilyMemberCommand(familyDocId: 1, memberDocId: 2);

            _repoMock.Setup(r => r.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), It.IsAny<IDbTransaction>(), "text"))
                     .ThrowsAsync(new Exception("Database Error")); // Simulate a database failure

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));

            // Verify that CommitAsync was not called since the transaction should not be committed on failure
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<IDbTransaction>()), Times.Never);
        }
    }
}
