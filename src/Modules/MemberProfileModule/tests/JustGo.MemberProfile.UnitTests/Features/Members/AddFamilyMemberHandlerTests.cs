using Dapper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.Features.Members.Commands.AddFamilyMember;
using JustGo.MemberProfile.Test.Helper;
using Moq;

namespace JustGo.MemberProfile.Test.Features.Members
{
    public class AddFamilyMemberHandlerTests
    {
        private readonly Mock<IWriteRepository<object>> _writeRepositoryMock;
        private readonly Mock<IWriteRepositoryFactory> _writeRepositoryFactoryMock;
        private readonly AddFamilyMemberHandler _handler;

        public AddFamilyMemberHandlerTests()
        {
            _writeRepositoryMock = new Mock<IWriteRepository<object>>();
            _writeRepositoryFactoryMock = new Mock<IWriteRepositoryFactory>();

            var lazyWriteRepo = LazyServiceMockHelper.MockLazyService(_writeRepositoryMock.Object);
            _writeRepositoryFactoryMock.Setup(f => f.GetLazyRepository<object>()).Returns(lazyWriteRepo);

            _handler = new AddFamilyMemberHandler(_writeRepositoryFactoryMock.Object);
        }

        [Fact]
        public async Task Handle_Should_ReturnAffectedRows_WhenExecutedSuccessfully()
        {
            // Arrange
            var command = new AddFamilyMemberCommand
            {
                Name = "Test Family",
                FamilyDocId = 0,
                ClubDocId = 101,
                UserId = 10,
                MemberDocIds = "201,202"
            };

            _writeRepositoryMock.Setup(r => r.ExecuteAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    null,
                    "sp"))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(1);

            _writeRepositoryMock.Verify(r => r.ExecuteAsync(
                "AddFamilyMember",
                It.IsAny<CancellationToken>(),
                It.IsAny<DynamicParameters>(),
                null,
                "sp"), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ThrowException_WhenExecutionFails()
        {
            // Arrange
            var command = new AddFamilyMemberCommand
            {
                Name = "Fail Test",
                FamilyDocId = 1,
                ClubDocId = 100,
                UserId = 9,
                MemberDocIds = "" // edge case
            };

            _writeRepositoryMock.Setup(r => r.ExecuteAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    null,
                    "sp"))
                .ThrowsAsync(new Exception("DB failure"));

            // Act
            Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await action.Should().ThrowAsync<Exception>().WithMessage("DB failure");
        }
    }
}
