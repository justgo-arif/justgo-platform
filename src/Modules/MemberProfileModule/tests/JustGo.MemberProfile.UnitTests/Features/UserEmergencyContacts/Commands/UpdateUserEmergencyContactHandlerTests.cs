using System.Data;
using Dapper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Commands.UpdateUserEmergencyContacts;
using JustGo.MemberProfile.Test.Helper;
using Moq;

public class UpdateUserEmergencyContactHandlerTests
{
    private readonly Mock<IWriteRepositoryFactory> _writeRepositoryFactoryMock = new();
    private readonly Mock<IWriteRepository<JustGo.MemberProfile.Domain.Entities.UserEmergencyContact>> _writeRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDbTransaction> _transactionMock = new();
    private UpdateUserEmergencyContactHandler _handler;

    public UpdateUserEmergencyContactHandlerTests()
    {
        var lazyRepo = LazyServiceMockHelper.MockLazyService(_writeRepositoryMock.Object);
        _writeRepositoryFactoryMock
            .Setup(x => x.GetLazyRepository<JustGo.MemberProfile.Domain.Entities.UserEmergencyContact>())
            .Returns(lazyRepo);

        _unitOfWorkMock
            .Setup(u => u.BeginTransactionAsync())
            .ReturnsAsync(_transactionMock.Object);

        _handler = new UpdateUserEmergencyContactHandler(_writeRepositoryFactoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_Should_UpdateContactOnly_WhenIsPrimaryIsNull()
    {
        // Arrange
        var command = new UpdateUserEmergencyContactCommand
        {
            FirstName = "John",
            LastName = "Doe",
            Relation = "Brother",
            ContactNumber = "1234567890",
            EmailAddress = "john.doe@example.com",
            IsPrimary = null,
            CountryCode = "BD",
            SyncGuid = "some-sync-guid"

        };

        int executeCalls = 0;
        List<string> capturedSqls = new();

        _writeRepositoryMock
            .Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<DynamicParameters>(),
                It.IsAny<IDbTransaction>(),
                "Text"))
            .Callback<string, CancellationToken, object, IDbTransaction, string>((sql, _, _, _, _) =>
            {
                executeCalls++;
                capturedSqls.Add(sql);
            })
            .ReturnsAsync(1);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<IDbTransaction>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(1);
        executeCalls.Should().Be(1);

        capturedSqls[0].Should().Contain("UPDATE [dbo].[UserEmergencyContacts]");

        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<IDbTransaction>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ThrowException_WhenUpdateFails()
    {
        // Arrange
        var command = new UpdateUserEmergencyContactCommand
        {
            FirstName = "John",
            LastName = "Doe",
            Relation = "Brother",
            ContactNumber = "1234567890",
            EmailAddress = "john.doe@example.com",
            IsPrimary = true,
            CountryCode = "BD",
            SyncGuid = "some-sync-guid" 

        };

        _writeRepositoryMock
            .Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<DynamicParameters>(),
                It.IsAny<IDbTransaction>(),
                "Text"))
            .ThrowsAsync(new System.Exception("DB update failed"));

        // Act & Assert
        await Assert.ThrowsAsync<System.Exception>(() => _handler.Handle(command, CancellationToken.None));

        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<IDbTransaction>()), Times.Never);
    }
}
