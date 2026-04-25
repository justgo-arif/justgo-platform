using Dapper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Application.Features.Members.Queries.SearchMembersForFamily;
using JustGo.MemberProfile.Test.Helper;
using Moq;

namespace JustGo.MemberProfile.Test.Features.Members;

public class SearchMembersForFamilyHandlerTests
{
    private readonly Mock<IReadRepositoryFactory> _readRepositoryFactoryMock;
    private readonly Mock<IReadRepository<FindMemberDto>> _readRepositoryMock;
    private readonly SearchMembersForFamilyHandler _handler;

    public SearchMembersForFamilyHandlerTests()
    {
        _readRepositoryFactoryMock = new Mock<IReadRepositoryFactory>();
        _readRepositoryMock = new Mock<IReadRepository<FindMemberDto>>();

        var lazyReadRepository = LazyServiceMockHelper.MockLazyService(_readRepositoryMock.Object);
        _readRepositoryFactoryMock
            .Setup(x => x.GetLazyRepository<FindMemberDto>())
            .Returns(lazyReadRepository);

        _handler = new SearchMembersForFamilyHandler(_readRepositoryFactoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedResults_WhenMatchingMembersExist()
    {
        // Arrange
        var query = new SearchMembersForFamilyQuery
        {
            Email = "test@example.com",
            MID = "MBR001"
            // DateOfBirth is optional here
        };

        var fakeData = new List<FindMemberDto>
        {
            new FindMemberDto
            {
                MemberDocId = 101,
                MID = "MBR001",
                UserId = 1,
                FirstName = "Alice",
                LastName = "Smith"
            },
            new FindMemberDto
            {
                MemberDocId = 102,
                MID = "MBR002",
                UserId = 2,
                FirstName = "Bob",
                LastName = "Johnson"
            }
        };

        _readRepositoryMock.Setup(r => r.GetListAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
            .ReturnsAsync(fakeData);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].FirstName.Should().Be("Alice");
        result[1].MID.Should().Be("MBR002");

        _readRepositoryMock.Verify(r => r.GetListAsync(
            It.Is<string>(sql => sql.Contains("SELECT") && sql.Contains("U.MemberId = @MID")),
            It.IsAny<CancellationToken>(),
            It.Is<DynamicParameters>(p =>
                p.Get<string>("Email") == "test@example.com" &&
                p.Get<string>("MID") == "MBR001"
            ),
            null,
            "text"), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoMatchingMembersExist()
    {
        // Arrange
        var query = new SearchMembersForFamilyQuery
        {
            Email = "none@example.com",
            DateOfBirth = new DateTime(2000, 1, 1)
            // MID is not set — valid case per business rule
        };

        _readRepositoryMock.Setup(r => r.GetListAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
            .ReturnsAsync(new List<FindMemberDto>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        _readRepositoryMock.Verify(r => r.GetListAsync(
            It.Is<string>(sql => sql.Contains("SELECT") && sql.Contains("U.DOB = @DOB")),
            It.IsAny<CancellationToken>(),
            It.Is<DynamicParameters>(p =>
                p.Get<string>("Email") == "none@example.com" &&
                p.Get<DateTime>("DOB") == new DateTime(2000, 1, 1)
            ),
            null,
            "text"), Times.Once);
    }
}