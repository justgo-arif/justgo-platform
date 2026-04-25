using System.Data;
using Dapper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Organisation.Application.Features.Organizations.Commands.SetPrimaryClub;
using JustGo.Organisation.Test.Helper;
using Moq;

namespace JustGo.Organisation.Test.Features.Organisations.Commands
{
    public class SetPrimaryClubHandlerTests
    {
        private readonly Mock<IWriteRepository<object>> _writeRepoMock;
        private readonly Mock<IWriteRepositoryFactory> _writeRepoFactoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IUtilityService> _utilityServiceMock;
        private readonly Mock<IReadRepositoryFactory> _readRepoFactoryMock;
        private readonly Mock<IReadRepository<string>> _readRepoStringMock;
        private readonly Mock<IDbTransaction> _transactionMock;
        private readonly SetPrimaryClubHandler _handler;

        public SetPrimaryClubHandlerTests()
        {
            _writeRepoMock = new Mock<IWriteRepository<object>>();
            _writeRepoFactoryMock = new Mock<IWriteRepositoryFactory>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _utilityServiceMock = new Mock<IUtilityService>();
            _readRepoFactoryMock = new Mock<IReadRepositoryFactory>();
            _readRepoStringMock = new Mock<IReadRepository<string>>();
            _transactionMock = new Mock<IDbTransaction>();

            // Setup lazy repo for write repository
            var lazyWriteRepo = LazyServiceMockHelper.MockLazyService(_writeRepoMock.Object);
            _writeRepoFactoryMock.Setup(f => f.GetLazyRepository<object>())
                            .Returns(lazyWriteRepo);

            // Setup lazy read repo for string (used for all read operations)
            var lazyReadRepoString = LazyServiceMockHelper.MockLazyService(_readRepoStringMock.Object);
            _readRepoFactoryMock.Setup(f => f.GetLazyRepository<string>())
                                .Returns(lazyReadRepoString);

            // Setup UnitOfWork transaction methods
            _unitOfWorkMock.Setup(u => u.BeginTransactionAsync())
                           .ReturnsAsync(_transactionMock.Object);
            _unitOfWorkMock.Setup(u => u.CommitAsync(It.IsAny<IDbTransaction>()))
                           .Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.RollbackAsync(It.IsAny<IDbTransaction>()))
                           .Returns(Task.CompletedTask);

            // Setup UtilityService to return a userId (simulate current user)
            _utilityServiceMock.Setup(u => u.GetCurrentUserId(It.IsAny<CancellationToken>()))
                               .ReturnsAsync(456);

            // Instantiate handler with all dependencies
            _handler = new SetPrimaryClubHandler(
                _writeRepoFactoryMock.Object,
                _unitOfWorkMock.Object,
                _utilityServiceMock.Object,
                _readRepoFactoryMock.Object);
        }



        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_MakePrimaryClub_ProcedureExists()
        {
            // Arrange
            var memberGuid = Guid.NewGuid();
            var command = new SetPrimaryClubCommand
            {
                ClubMemberId = 101,
                MemberSyncGuid = memberGuid
            };

            // Mock GetSingleAsync for getting memberDocId from Document table (returns string "202")
            _readRepoStringMock.Setup(r => r.GetSingleAsync(
                It.Is<string>(s => s.Contains("select docId from Document")),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                "text"))
                .ReturnsAsync((object)202);

            // Mock procedure existence checks using SetupSequence
            // First call: MakePrimaryClub exists (returns 1)
            // Second call: SetUserCurrency exists (returns 1)
            _readRepoStringMock.SetupSequence(r => r.GetSingleAsync(
                It.Is<string>(s => s.Contains("sys.procedures") && s.Contains("@ProcName")),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                "text"))
                .ReturnsAsync((object)1)  // MakePrimaryClub exists
                .ReturnsAsync((object)1); // SetUserCurrency exists

            // Mock MakePrimaryClub execution
            DynamicParameters? capturedMakePrimaryParams = null;
            _writeRepoMock.Setup(r => r.ExecuteAsync(
                "MakePrimaryClub",
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                "sp"))
                .Callback<string, CancellationToken, object, IDbTransaction, string>((_, _, param, _, _) =>
                {
                    capturedMakePrimaryParams = param as DynamicParameters;
                })
                .ReturnsAsync(1);

            // Mock SetUserCurrency execution
            _writeRepoMock.Setup(r => r.ExecuteAsync(
                "SetUserCurrency",
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                "sp"))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Set primary request was processed successfully.");

            // Verify MakePrimaryClub was called with correct parameters
            capturedMakePrimaryParams.Should().NotBeNull();
            capturedMakePrimaryParams.Get<int>("@ClubMemberDocId").Should().Be(101);
            capturedMakePrimaryParams.Get<int>("@MemberDocId").Should().Be(202);
            capturedMakePrimaryParams.Get<int>("@ActionUserId").Should().Be(456);

            _unitOfWorkMock.Verify(u => u.CommitAsync(_transactionMock.Object), Times.Once);
            _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<IDbTransaction>()), Times.Never);
        }


        [Fact]
        public async Task Handle_Should_UseFallbackLogic_When_MakePrimaryClub_DoesNotExist()
        {
            // Arrange
            var memberGuid = Guid.NewGuid();
            var command = new SetPrimaryClubCommand
            {
                ClubMemberId = 101,
                MemberSyncGuid = memberGuid
            };

            //GetSingleAsync for getting memberDocId from Document table
            _readRepoStringMock.Setup(r => r.GetSingleAsync(
                It.Is<string>(s => s.Contains("select docId from Document")),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                "text"))
                .ReturnsAsync((object)202);

            //procedure existence check - MakePrimaryClub does NOT exist
            _readRepoStringMock.Setup(r => r.GetSingleAsync(
                It.Is<string>(s => s.Contains("sys.procedures")),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                "text"))
                .ReturnsAsync((object)0);

            //first UPDATE query (set IsPrimary = 1)
            _writeRepoMock.Setup(r => r.ExecuteAsync(
                It.Is<string>(s => s.Contains("UPDATE ClubMembers_Default SET IsPrimary = 1")),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                "text"))
                .ReturnsAsync(1);

            //second UPDATE query (set IsPrimary = 0 for others)
            _writeRepoMock.Setup(r => r.ExecuteAsync(
                It.Is<string>(s => s.Contains("UPDATE ClubMembers_Default") && s.Contains("SET IsPrimary = 0")),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                "text"))
                .ReturnsAsync(1);

            //procedure existence check - SetUserCurrency does NOT exist
            _readRepoStringMock.Setup(r => r.GetSingleAsync(
                It.Is<string>(s => s.Contains("sys.procedures") && s.Contains("SetUserCurrency")),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                "text"))
                .ReturnsAsync((object)0);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Be("Set primary request was processed successfully.");

            _writeRepoMock.Verify(r => r.ExecuteAsync(
                It.Is<string>(s => s.Contains("UPDATE ClubMembers_Default SET IsPrimary = 1")),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                _transactionMock.Object,
                "text"), Times.Once);

            _writeRepoMock.Verify(r => r.ExecuteAsync(
                It.Is<string>(s => s.Contains("UPDATE ClubMembers_Default") && s.Contains("SET IsPrimary = 0")),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                _transactionMock.Object,
                "text"), Times.Once);

            //SetUserCurrency was NOT called
            _writeRepoMock.Verify(r => r.ExecuteAsync(
                "SetUserCurrency",
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                "sp"), Times.Never);

            _unitOfWorkMock.Verify(u => u.CommitAsync(_transactionMock.Object), Times.Once);
            _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<IDbTransaction>()), Times.Never);
        }


        [Fact]
        public async Task Handle_Should_ReturnFailure_WhenExceptionOccurs()
        {
            // Arrange
            var memberGuid = Guid.NewGuid();
            var command = new SetPrimaryClubCommand
            {
                ClubMemberId = 101,
                MemberSyncGuid = memberGuid
            };

            var exceptionMessage = "Database connection error";

            //GetSingleAsync to throw exception
            _readRepoStringMock.Setup(r => r.GetSingleAsync(
                It.Is<string>(s => s.Contains("select docId from Document")),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                "text"))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be(exceptionMessage);

            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<IDbTransaction>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.RollbackAsync(_transactionMock.Object), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_CallSetUserCurrency_WhenProcedureExists()
        {
            // Arrange
            var memberGuid = Guid.NewGuid();
            var command = new SetPrimaryClubCommand
            {
                ClubMemberId = 101,
                MemberSyncGuid = memberGuid
            };

            //getting memberDocId from Document
            _readRepoStringMock.Setup(r => r.GetSingleAsync(
                It.Is<string>(s => s.Contains("select docId from Document")),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                "text"))
                .ReturnsAsync((object)202);

            //procedure existence check MakePrimaryClub
            _readRepoStringMock.Setup(r => r.GetSingleAsync(
                It.Is<string>(s => s.Contains("sys.procedures")),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                "text"))
                .ReturnsAsync((object)1);

            _writeRepoMock.Setup(r => r.ExecuteAsync(
                "MakePrimaryClub",
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                "sp"))
                .ReturnsAsync(1);

            //procedure existence check
            _readRepoStringMock.Setup(r => r.GetSingleAsync(
                It.Is<string>(s => s.Contains("sys.procedures") && s.Contains("SetUserCurrency")),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                "text"))
                .ReturnsAsync((object)1);

            _writeRepoMock.Setup(r => r.ExecuteAsync(
                "SetUserCurrency",
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                "sp"))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();

            //Verify SetUserCurrency was called
            _writeRepoMock.Verify(r => r.ExecuteAsync(
                "SetUserCurrency",
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                _transactionMock.Object,
                "sp"), Times.Once);
        }

    }
}
