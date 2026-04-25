using Dapper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Organisation.Application.Features.Organizations.Commands.ClubTransferRequest;
using JustGo.Organisation.Application.Features.Transfers.Handlers;
using JustGo.Organisation.Test.Helper;
using Moq;
using System.Data;

namespace JustGo.Organisation.Test.Features.Organisations.Commands
{
    public class ClubTransferRequestHandlerTests
    {
        private readonly Mock<IWriteRepository<object>> _writeRepoMock;
        private readonly Mock<IWriteRepositoryFactory> _writeRepoFactoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IUtilityService> _utilityServiceMock;
        private readonly ClubTransferRequestHandler _handler;

        public ClubTransferRequestHandlerTests()
        {
            _writeRepoMock = new Mock<IWriteRepository<object>>();
            _writeRepoFactoryMock = new Mock<IWriteRepositoryFactory>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _utilityServiceMock = new Mock<IUtilityService>();

            var lazyWriteRepo = LazyServiceMockHelper.MockLazyService(_writeRepoMock.Object);
            _writeRepoFactoryMock.Setup(f => f.GetLazyRepository<object>()).Returns(lazyWriteRepo);

            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(Mock.Of<IDbTransaction>());
            _unitOfWorkMock.Setup(u => u.CommitAsync(It.IsAny<IDbTransaction>())).Returns(Task.CompletedTask);

            _utilityServiceMock.Setup(u => u.GetCurrentUserId(It.IsAny<CancellationToken>()))
                               .ReturnsAsync(123);

            _handler = new ClubTransferRequestHandler(
                _writeRepoFactoryMock.Object,
                _unitOfWorkMock.Object,
                _utilityServiceMock.Object
            );
        }

        [Fact]
        public async Task Handle_Should_ReturnTransferDocId_WhenValid()
        {
            // Arrange
            var command = new ClubTransferRequestCommand
            {
                FromClubSyncGuid = Guid.NewGuid(),
                ToClubSyncGuid = Guid.NewGuid(),
                MemberSyncGuid = Guid.NewGuid(),
                ReasonForMove = "New location"
            };

            _writeRepoMock.Setup(r => r.ExecuteAsync("IsMemberValidForClubMemberAdd", It.IsAny<CancellationToken>(), It.IsAny<object>(), It.IsAny<IDbTransaction>(), "sp"))
                .Callback<string, CancellationToken, object, IDbTransaction, string>((_, _, param, _, _) =>
                {
                    var dp = param as DynamicParameters;
                    dp?.Add("@Result", null, DbType.String, ParameterDirection.Output, 500);
                })
                .ReturnsAsync(0);

            _writeRepoMock.Setup(r => r.ExecuteAsync("CreateTransferDocumentBySyncGuid", It.IsAny<CancellationToken>(), It.IsAny<object>(), It.IsAny<IDbTransaction>(), "sp"))
                .Callback<string, CancellationToken, object, IDbTransaction, string>((_, _, param, _, _) =>
                {
                    var dp = param as DynamicParameters;
                    dp?.Add("@trDocId", 456, DbType.Int32, ParameterDirection.Output);
                })
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(456);
        }

        [Fact]
        public async Task Handle_Should_ReturnFailure_WhenValidationFails()
        {
            // Arrange
            var command = new ClubTransferRequestCommand
            {
                FromClubSyncGuid = Guid.NewGuid(),
                ToClubSyncGuid = Guid.NewGuid(),
                MemberSyncGuid = Guid.NewGuid(),
                ReasonForMove = "Relocation"
            };

            _writeRepoMock.Setup(r => r.ExecuteAsync("IsMemberValidForClubMemberAdd", It.IsAny<CancellationToken>(), It.IsAny<object>(), It.IsAny<IDbTransaction>(), "sp"))
                .Callback<string, CancellationToken, object, IDbTransaction, string>((_, _, param, _, _) =>
                {
                    var dp = param as DynamicParameters;
                    dp?.Add("@Result", "Validation failed", DbType.String, ParameterDirection.Output, 500);
                })
                .ReturnsAsync(0);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("Validation failed");
        }
    }
}
