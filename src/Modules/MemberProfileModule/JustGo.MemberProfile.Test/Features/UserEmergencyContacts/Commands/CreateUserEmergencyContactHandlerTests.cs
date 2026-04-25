using System.Data;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Commands.CreateUserEmergencyContacts;
using JustGo.MemberProfile.Test.Helper;
using Moq;

public class CreateUserEmergencyContactHandlerTests
{
    private readonly Mock<IWriteRepositoryFactory> _writeRepositoryFactoryMock = new();
    private readonly Mock<IWriteRepository<JustGo.MemberProfile.Domain.Entities.UserEmergencyContact>> _writeRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDbTransaction> _transactionMock = new();
    private CreateUserEmergencyContactHandler _handler;

    public CreateUserEmergencyContactHandlerTests()
    {
        var lazyRepo = LazyServiceMockHelper.MockLazyService(_writeRepositoryMock.Object);

        _writeRepositoryFactoryMock
            .Setup(x => x.GetLazyRepository<JustGo.MemberProfile.Domain.Entities.UserEmergencyContact>())
            .Returns(lazyRepo);

        _unitOfWorkMock
            .Setup(u => u.BeginTransactionAsync())
            .ReturnsAsync(_transactionMock.Object);

        _handler = new CreateUserEmergencyContactHandler(_writeRepositoryFactoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_InsertContactAndSetPrimary_WhenIsPrimaryTrue()
    {
        var command = new CreateUserEmergencyContactCommand
        {
            UserSyncGuid = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            FirstName = "John",
            LastName = "Doe",
            Relation = "Brother",
            ContactNumber = "1234567890",
            EmailAddress = "john.doe@example.com",
            IsPrimary = true,
            CountryCode = "BD"
        };

        int executeCalls = 0;
        List<string> capturedSqls = new();

        _writeRepositoryMock
            .Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                It.IsAny<string>()))
            .Callback<string, CancellationToken, object, IDbTransaction, string>((sql, _, _, tx, _) =>
            {
                executeCalls++;
                capturedSqls.Add(sql);
                tx.Should().Be(_transactionMock.Object);
            })
            .ReturnsAsync(1);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<IDbTransaction>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(1);

        capturedSqls[0].Should().Contain("INSERT INTO [dbo].[UserEmergencyContacts]");

        _unitOfWorkMock.Verify(u => u.CommitAsync(_transactionMock.Object), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_InsertContactOnly_WhenIsPrimaryFalse()
    {
        var command = new CreateUserEmergencyContactCommand
        {
            UserSyncGuid = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            FirstName = "John",
            LastName = "Doe",
            Relation = "Brother",
            ContactNumber = "1234567890",
            EmailAddress = "john.doe@example.com",
            IsPrimary = false,
            CountryCode = "BD"
        };

        int executeCalls = 0;
        List<string> capturedSqls = new();

        _writeRepositoryMock
            .Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                It.IsAny<string>()))
            .Callback<string, CancellationToken, object, IDbTransaction, string>((sql, _, _, tx, _) =>
            {
                executeCalls++;
                capturedSqls.Add(sql);
                tx.Should().Be(_transactionMock.Object);
            })
            .ReturnsAsync(1);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<IDbTransaction>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(1);
        executeCalls.Should().Be(1);

        capturedSqls[0].Should().Contain("INSERT INTO [dbo].[UserEmergencyContacts]");

        _unitOfWorkMock.Verify(u => u.CommitAsync(_transactionMock.Object), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_InsertContactOnly_WhenIsPrimaryIsNull()
    {
        var command = new CreateUserEmergencyContactCommand
        {
            UserSyncGuid = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            FirstName = "John",
            LastName = "Doe",
            Relation = "Brother",
            ContactNumber = "1234567890",
            EmailAddress = "john.doe@example.com",
            IsPrimary = null,
            CountryCode = "BD"
        };

        int executeCalls = 0;
        List<string> capturedSqls = new();

        _writeRepositoryMock
            .Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                It.IsAny<string>()))
            .Callback<string, CancellationToken, object, IDbTransaction, string>((sql, _, _, tx, _) =>
            {
                executeCalls++;
                capturedSqls.Add(sql);
                tx.Should().Be(_transactionMock.Object);
            })
            .ReturnsAsync(1);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<IDbTransaction>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(1);
        executeCalls.Should().Be(1);

        capturedSqls[0].Should().Contain("INSERT INTO [dbo].[UserEmergencyContacts]");

        _unitOfWorkMock.Verify(u => u.CommitAsync(_transactionMock.Object), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowException_WhenInsertFails()
    {
        var command = new CreateUserEmergencyContactCommand
        {
            UserSyncGuid = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            FirstName = "John",
            LastName = "Doe",
            Relation = "Brother",
            ContactNumber = "1234567890",
            EmailAddress = "john.doe@example.com",
            IsPrimary = true,
            CountryCode = "BD"
        };

        _writeRepositoryMock
            .Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                It.IsAny<IDbTransaction>(),
                It.IsAny<string>()))
            .ThrowsAsync(new System.Exception("DB insert failed"));

        await Assert.ThrowsAsync<System.Exception>(() => _handler.Handle(command, CancellationToken.None));

        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<IDbTransaction>()), Times.Never);
    }
}
