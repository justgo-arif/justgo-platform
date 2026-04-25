using Dapper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.Features.MemberFamily.Queries.GetFamilySummary;
using JustGo.MemberProfile.Domain.Entities;
using JustGo.MemberProfile.Test.Helper;
using Moq;
using System.Data;

namespace JustGo.MemberProfile.Test.Features.MemberFamily;

public class GetFamilySummaryHandlerTests
{
    private readonly Mock<IReadRepositoryFactory> _readRepositoryFactoryMock;
    private readonly Mock<IReadRepository<object>> _readRepositoryMock;
    private readonly Mock<IMultipleResultReader> _multipleResultReaderMock;
    private readonly GetFamilySummaryHandler _handler;

    public GetFamilySummaryHandlerTests()
    {
        _readRepositoryFactoryMock = new Mock<IReadRepositoryFactory>();
        _readRepositoryMock = new Mock<IReadRepository<object>>();
        _multipleResultReaderMock = new Mock<IMultipleResultReader>();

        var lazyReadRepo = LazyServiceMockHelper.MockLazyService(_readRepositoryMock.Object);
        _readRepositoryFactoryMock
            .Setup(x => x.GetLazyRepository<object>())
            .Returns(lazyReadRepo);

        _handler = new GetFamilySummaryHandler(
            _readRepositoryFactoryMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnFamilyWithMembers_WhenFamilyExists()
    {
        // Arrange
        var userSyncId = Guid.NewGuid();
        var familyRecordGuid = Guid.NewGuid();

        var query = new GetFamilySummaryQuery(userSyncId);

        var expectedFamily = new Family
        {
            Reference = "FAM001",
            FamilyName = "Smith Family",
            RecordGuid = familyRecordGuid
        };

        var expectedMembers = new List<FamilyMember>
        {
            new FamilyMember
            {
                UserFamilyId = 1,
                FamilyId = 1,
                IsAdmin = true,
                FirstName = "John",
                LastName = "Smith",
                MemberId = "M001",
                EmailAddress = "john.smith@example.com",
                UserSyncId = userSyncId,
                ProfilePicURL = "/store/download?f=profile.jpg&t=user&p=123"
            },
            new FamilyMember
            {
                UserFamilyId = 2,
                FamilyId = 1,
                IsAdmin = false,
                FirstName = "Jane",
                LastName = "Smith",
                MemberId = "M002",
                EmailAddress = "jane.smith@example.com",
                UserSyncId = Guid.NewGuid(),
                ProfilePicURL = null
            }
        };

        _multipleResultReaderMock
            .Setup(g => g.ReadSingleOrDefaultAsync<Family>())
            .ReturnsAsync(expectedFamily);

        _multipleResultReaderMock
            .Setup(g => g.ReadAsync<FamilyMember>())
            .ReturnsAsync(expectedMembers);

        _readRepositoryMock
            .Setup(r => r.GetMultipleQueryAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
            .ReturnsAsync(_multipleResultReaderMock.Object);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.FamilyName.Should().Be("Smith Family");
        result.Reference.Should().Be("FAM001");
        result.RecordGuid.Should().Be(familyRecordGuid);
        result.Members.Should().HaveCount(2);
        result.Members.Should().Contain(m => m.IsAdmin && m.FirstName == "John");
        result.Members.Should().Contain(m => !m.IsAdmin && m.FirstName == "Jane");
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenMultipleResultReaderIsNull()
    {
        // Arrange
        var userSyncId = Guid.NewGuid();
        var query = new GetFamilySummaryQuery(userSyncId);

        _readRepositoryMock
            .Setup(r => r.GetMultipleQueryAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
            .ReturnsAsync(_multipleResultReaderMock.Object);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenFamilyDoesNotExist()
    {
        // Arrange
        var userSyncId = Guid.NewGuid();
        var query = new GetFamilySummaryQuery(userSyncId);

        _multipleResultReaderMock
            .Setup(g => g.ReadSingleOrDefaultAsync<Family>())
            .ReturnsAsync((Family?)null);

        _readRepositoryMock
            .Setup(r => r.GetMultipleQueryAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
            .ReturnsAsync(_multipleResultReaderMock.Object);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnFamilyWithEmptyMembers_WhenNoMembersExist()
    {
        // Arrange
        var userSyncId = Guid.NewGuid();
        var query = new GetFamilySummaryQuery(userSyncId);

        var expectedFamily = new Family
        {
            Reference = "FAM001",
            FamilyName = "Smith Family",
            RecordGuid = Guid.NewGuid()
        };

        _multipleResultReaderMock
            .Setup(g => g.ReadSingleOrDefaultAsync<Family>())
            .ReturnsAsync(expectedFamily);

        _multipleResultReaderMock
            .Setup(g => g.ReadAsync<FamilyMember>())
            .ReturnsAsync(Enumerable.Empty<FamilyMember>());

        _readRepositoryMock
            .Setup(r => r.GetMultipleQueryAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
            .ReturnsAsync(_multipleResultReaderMock.Object);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.FamilyName.Should().Be("Smith Family");
        result.Members.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldIncludeCorrectUserSyncIdInQuery_WhenCalled()
    {
        // Arrange
        var userSyncId = Guid.NewGuid();
        var query = new GetFamilySummaryQuery(userSyncId);
        DynamicParameters? capturedParameters = null;

        var expectedFamily = new Family
        {
            FamilyName = "Test Family",
            RecordGuid = Guid.NewGuid()
        };

        _multipleResultReaderMock
            .Setup(g => g.ReadSingleOrDefaultAsync<Family>())
            .ReturnsAsync(expectedFamily);

        _multipleResultReaderMock
            .Setup(g => g.ReadAsync<FamilyMember>())
            .ReturnsAsync(new List<FamilyMember>());

        _readRepositoryMock
            .Setup(r => r.GetMultipleQueryAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
            .Callback<string, CancellationToken, object?, IDbTransaction?, string?>((sql, ct, param, trans, type) =>
            {
                capturedParameters = param as DynamicParameters;
            })
            .ReturnsAsync(_multipleResultReaderMock.Object);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedParameters.Should().NotBeNull();
        var paramValue = capturedParameters!.Get<Guid>("UserSyncId");
        paramValue.Should().Be(userSyncId);
    }

    [Fact]
    public async Task Handle_ShouldExecuteCorrectSqlQuery_WhenCalled()
    {
        // Arrange
        var userSyncId = Guid.NewGuid();
        var query = new GetFamilySummaryQuery(userSyncId);
        string? capturedSql = null;

        var expectedFamily = new Family
        {
            FamilyName = "Test Family",
            RecordGuid = Guid.NewGuid()
        };

        _multipleResultReaderMock
            .Setup(g => g.ReadSingleOrDefaultAsync<Family>())
            .ReturnsAsync(expectedFamily);

        _multipleResultReaderMock
            .Setup(g => g.ReadAsync<FamilyMember>())
            .ReturnsAsync(new List<FamilyMember>());

        _readRepositoryMock
            .Setup(r => r.GetMultipleQueryAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
            .Callback<string, CancellationToken, object?, IDbTransaction?, string?>((sql, ct, param, trans, type) =>
            {
                capturedSql = sql;
            })
            .ReturnsAsync(_multipleResultReaderMock.Object);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedSql.Should().NotBeNull();
        capturedSql.Should().Contain("DECLARE @FamilyId INT");
        capturedSql.Should().Contain("UserFamilies");
        capturedSql.Should().Contain("Families");
        capturedSql.Should().Contain("@UserSyncId");
    }

    [Fact]
    public async Task Handle_ShouldHandleCancellationToken_WhenProvided()
    {
        // Arrange
        var userSyncId = Guid.NewGuid();
        var query = new GetFamilySummaryQuery(userSyncId);
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _readRepositoryMock
            .Setup(r => r.GetMultipleQueryAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
            .ReturnsAsync(_multipleResultReaderMock.Object);

        // Act
        var result = await _handler.Handle(query, cancellationToken);

        // Assert
        _readRepositoryMock.Verify(
            r => r.GetMultipleQueryAsync(
                It.IsAny<string>(),
                cancellationToken,
                It.IsAny<object>(),
                null,
                "text"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFamilyWithMultipleMembers_WhenMultipleMembersExist()
    {
        // Arrange
        var userSyncId = Guid.NewGuid();
        var query = new GetFamilySummaryQuery(userSyncId);

        var expectedFamily = new Family
        {
            Reference = "FAM005",
            FamilyName = "Johnson Family",
            RecordGuid = Guid.NewGuid()
        };

        var expectedMembers = new List<FamilyMember>
        {
            new FamilyMember { UserFamilyId = 10, FamilyId = 5, IsAdmin = true, FirstName = "Alice", LastName = "Johnson", MemberId = "M010", UserSyncId = Guid.NewGuid(), EmailAddress = "alice@test.com" },
            new FamilyMember { UserFamilyId = 11, FamilyId = 5, IsAdmin = false, FirstName = "Bob", LastName = "Johnson", MemberId = "M011", UserSyncId = Guid.NewGuid(), EmailAddress = "bob@test.com" },
            new FamilyMember { UserFamilyId = 12, FamilyId = 5, IsAdmin = false, FirstName = "Charlie", LastName = "Johnson", MemberId = "M012", UserSyncId = Guid.NewGuid(), EmailAddress = "charlie@test.com" },
            new FamilyMember { UserFamilyId = 13, FamilyId = 5, IsAdmin = false, FirstName = "Diana", LastName = "Johnson", MemberId = "M013", UserSyncId = Guid.NewGuid(), EmailAddress = "diana@test.com" }
        };

        _multipleResultReaderMock
            .Setup(g => g.ReadSingleOrDefaultAsync<Family>())
            .ReturnsAsync(expectedFamily);

        _multipleResultReaderMock
            .Setup(g => g.ReadAsync<FamilyMember>())
            .ReturnsAsync(expectedMembers);

        _readRepositoryMock
            .Setup(r => r.GetMultipleQueryAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
            .ReturnsAsync(_multipleResultReaderMock.Object);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Members.Should().HaveCount(4);
        result.Members.Count(m => m.IsAdmin).Should().Be(1);
        result.Members.Count(m => !m.IsAdmin).Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldPreserveAllFamilyProperties_WhenFamilyIsReturned()
    {
        // Arrange
        var userSyncId = Guid.NewGuid();
        var query = new GetFamilySummaryQuery(userSyncId);
        var recordGuid = Guid.NewGuid();

        var expectedFamily = new Family
        {
            Reference = "REF999",
            FamilyName = "Test Family Name",
            RecordGuid = recordGuid
        };

        _multipleResultReaderMock
            .Setup(g => g.ReadSingleOrDefaultAsync<Family>())
            .ReturnsAsync(expectedFamily);

        _multipleResultReaderMock
            .Setup(g => g.ReadAsync<FamilyMember>())
            .ReturnsAsync(new List<FamilyMember>());

        _readRepositoryMock
            .Setup(r => r.GetMultipleQueryAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
            .ReturnsAsync(_multipleResultReaderMock.Object);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Reference.Should().Be("REF999");
        result.FamilyName.Should().Be("Test Family Name");
        result.RecordGuid.Should().Be(recordGuid);
    }
}