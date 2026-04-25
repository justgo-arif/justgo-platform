using Dapper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Application.Features.Members.Commands.UpdateMember;
using JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberSummaryBySyncGuid;
using JustGo.MemberProfile.Domain.Entities;
using JustGo.MemberProfile.Test.Helper;
using Moq;
using System.Data;

namespace JustGo.MemberProfile.Test.Features.Members;

public class UpdateMemberHandlerTests
{
    private readonly Mock<IWriteRepositoryFactory> _writeRepositoryFactoryMock = new();
    private readonly Mock<IWriteRepository<MemberSummary>> _memberWriteRepositoryMock = new();
    private readonly Mock<IWriteRepository<UserPhoneNumber>> _phoneWriteRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IDbTransaction> _dbTransactionMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();

    private readonly UpdateMemberHandler _handler;

    public UpdateMemberHandlerTests()
    {
        var lazyServiceMember = LazyServiceMockHelper.MockLazyService(_memberWriteRepositoryMock.Object);
        var lazyServicePhone = LazyServiceMockHelper.MockLazyService(_phoneWriteRepositoryMock.Object);

        _writeRepositoryFactoryMock.Setup(x => x.GetLazyRepository<MemberSummary>())
            .Returns(lazyServiceMember);

        _writeRepositoryFactoryMock.Setup(x => x.GetLazyRepository<UserPhoneNumber>())
            .Returns(lazyServicePhone);

        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync())
            .ReturnsAsync(_dbTransactionMock.Object);
        _unitOfWorkMock.Setup(x => x.CommitAsync(_dbTransactionMock.Object)).Returns(Task.CompletedTask);

        // Setup mediator to return a sample MemberSummaryDto
        _mediatorMock.Setup(x => x.Send(It.IsAny<GetMemberSummaryBySyncGuidQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemberSummaryDto
            {
                LoginId = "user1",
                FirstName = "John",
                LastName = "Doe",
                MemberId = "member1"
            });

        _handler = new UpdateMemberHandler(_writeRepositoryFactoryMock.Object, _unitOfWorkMock.Object, _mediatorMock.Object);
    }

    [Fact]
    public async Task Handle_Should_UpdateMemberAndPhoneNumber_And_CommitTransaction()
    {
        // Arrange
        var command = new UpdateMemberCommand
        {
            UserSyncId = Guid.NewGuid(),
            LoginId = "user1",
            FirstName = "John",
            LastName = "Doe",
            Mobile = "1234567890",
            EmailAddress = "john@example.com",
            DOB = DateTime.UtcNow,
            Gender = "Male",
            Address1 = "Address1",
            Address2 = "Address2",
            Address3 = "Address3",
            Town = "Town",
            County = "County",
            Country = "Country",
            PostCode = "12345",
            CountryId = 1,
            CountyId = 2
        };

        _memberWriteRepositoryMock.Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<DynamicParameters>(),
                _dbTransactionMock.Object,
                "text"))
            .ReturnsAsync(1);

        _phoneWriteRepositoryMock.Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<DynamicParameters>(),
                _dbTransactionMock.Object,
                "text"))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("Member updated successfully");
        result.RowsAffected.Should().Be(1);
        result.Data.Should().NotBeNull();

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(), Times.Once);

        _memberWriteRepositoryMock.Verify(x => x.ExecuteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<DynamicParameters>(),
            _dbTransactionMock.Object,
            "text"), Times.Once);

        _unitOfWorkMock.Verify(x => x.CommitAsync(_dbTransactionMock.Object), Times.Once);

        _mediatorMock.Verify(x => x.Send(It.IsAny<GetMemberSummaryBySyncGuidQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_And_Rollback_When_UpdateMemberFails()
    {
        // Arrange
        var command = new UpdateMemberCommand
        {
            UserSyncId = Guid.NewGuid(),
            LoginId = "user1",
            FirstName = "John",
            LastName = "Doe",
            EmailAddress = "john@example.com",
            Gender = "Male",
            DOB = DateTime.UtcNow,
            Country = "USA"
        };

        var exceptionMessage = "DB Error";

        // Ensure RollbackAsync returns a Task to avoid NullReferenceException when awaited in the handler
        _unitOfWorkMock.Setup(x => x.RollbackAsync(It.IsAny<IDbTransaction>()))
            .Returns(Task.CompletedTask);

        _memberWriteRepositoryMock.Setup(x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<DynamicParameters>(),
                _dbTransactionMock.Object,
                "text"))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(exceptionMessage);
        result.RowsAffected.Should().Be(0);

        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<IDbTransaction>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<IDbTransaction>()), Times.Once);
    }

}