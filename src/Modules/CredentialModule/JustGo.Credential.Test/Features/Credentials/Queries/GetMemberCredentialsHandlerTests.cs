using Dapper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Credential.Application.DTOs;
using JustGo.Credential.Application.Features.Credentials.Queries.GetMemberCredentials;
using JustGo.Credential.Test.Helper;
using Moq;
using System.Data;

namespace JustGo.Credential.Test.Features.Credentials.Queries;

public class GetMemberCredentialsHandlerTests
{
    private readonly Mock<IReadRepository<CredentialsDto>> _repoMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly GetMemberCredentialsHandler _handler;

    public GetMemberCredentialsHandlerTests()
    {
        _repoMock = new Mock<IReadRepository<CredentialsDto>>();
        _mediatorMock = new Mock<IMediator>();
        var lazyRepo = LazyServiceMockHelper.MockLazyService(_repoMock.Object);
        _handler = new GetMemberCredentialsHandler(lazyRepo, _mediatorMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnCredentials_WhenDataExists()
    {
        // Arrange
        var userGuid = Guid.NewGuid();
        var request = new GetMemberCredentialsQuery
        {
            UserGuid = userGuid,
            Category = "All"
        };

        var expectedResult = new List<CredentialsDto>
        {
            new CredentialsDto 
            { 
                MemberCredentialId = 101, 
                Name = "Cert A", 
                ShortName = "CA",
                CredentialsType = "TypeA"
            },
            new CredentialsDto 
            { 
                MemberCredentialId = 102, 
                Name = "Cert B", 
                ShortName = "CB",
                CredentialsType = "TypeB"
            }
        };

        _repoMock.Setup(r => r.GetListAsync(
                It.Is<string>(sql => sql.Contains("FROM UC") && sql.Contains("UserSyncId")),
                It.IsAny<CancellationToken>(),
                It.Is<object>(p => ((DynamicParameters)p).Get<Guid>("@UserSyncId") == userGuid),
                It.IsAny<IDbTransaction>(),
                "text"))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("Credentials retrieved successfully.");
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(2);
        result.Data.Select(r => r.MemberCredentialId).Should().Contain(new[] { 101, 102 });
        result.RowsAffected.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoCredentialsFound()
    {
        // Arrange
        var userGuid = Guid.NewGuid();
        var request = new GetMemberCredentialsQuery
        {
            UserGuid = userGuid,
            Category = "All"
        };

        _repoMock.Setup(r => r.GetListAsync(
                It.Is<string>(sql => sql.Contains("FROM UC") && sql.Contains("UserSyncId")),
                It.IsAny<CancellationToken>(),
                It.Is<object>(p => ((DynamicParameters)p).Get<Guid>("@UserSyncId") == userGuid),
                It.IsAny<IDbTransaction>(),
                "text"))
            .ReturnsAsync(new List<CredentialsDto>());

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("Credentials retrieved successfully.");
        result.Data.Should().NotBeNull();
        result.Data.Should().BeEmpty();
        result.RowsAffected.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnCredentialsWithCorrectProperties()
    {
        // Arrange
        var userGuid = Guid.NewGuid();
        var request = new GetMemberCredentialsQuery
        {
            UserGuid = userGuid,
            Category = "All"
        };

        var expectedResult = new List<CredentialsDto>
        {
            new CredentialsDto 
            { 
                MemberCredentialId = 101, 
                Name = "Cert A",
                Description = "Description A",
                CredentialsType = "Type A",
                ShortName = "CA",
                IsLocked = false,
                Status = 1,
                CredentialMasterId = 50,
                StateId = 2,
                StateName = "Active"
            }
        };

        _repoMock.Setup(r => r.GetListAsync(
                It.Is<string>(sql => sql.Contains("FROM UC") && sql.Contains("UserSyncId")),
                It.IsAny<CancellationToken>(),
                It.Is<object>(p => ((DynamicParameters)p).Get<Guid>("@UserSyncId") == userGuid),
                It.IsAny<IDbTransaction>(),
                "text"))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        
        var credential = result.Data.First();
        credential.MemberCredentialId.Should().Be(101);
        credential.Name.Should().Be("Cert A");
        credential.Description.Should().Be("Description A");
        credential.CredentialsType.Should().Be("Type A");
        credential.ShortName.Should().Be("CA");
        credential.IsLocked.Should().BeFalse();
        credential.Status.Should().Be(1);
        credential.StateName.Should().Be("Active");
    }

}
