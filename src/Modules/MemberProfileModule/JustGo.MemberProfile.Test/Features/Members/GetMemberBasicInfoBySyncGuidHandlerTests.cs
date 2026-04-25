using Dapper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.Features.Members.Queries.GetMemberBasicInfoBySyncGuid;
using JustGo.MemberProfile.Domain.Entities;
using JustGo.MemberProfile.Test.Helper;
using MapsterMapper;
using Moq;
using System.Text.RegularExpressions;

namespace JustGo.MemberProfile.Test.Features.Members;

public class GetMemberBasicInfoBySyncGuidHandlerTests
{
    private readonly Mock<IReadRepository<MemberBasicInfo>> _readRepositoryMock;
    //private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetMemberBasicInfoBySyncGuidHandler _handler;

    public GetMemberBasicInfoBySyncGuidHandlerTests()
    {
        _readRepositoryMock = new Mock<IReadRepository<MemberBasicInfo>>();
        //_serviceProviderMock = new Mock<IServiceProvider>();
        _mapperMock = new Mock<IMapper>();

        var lazyReadRepository = LazyServiceMockHelper.MockLazyService(_readRepositoryMock.Object);
        _handler = new GetMemberBasicInfoBySyncGuidHandler(lazyReadRepository, _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnMember_WhenSyncGuidExists()
    {
        // Arrange
        var syncGuid = Guid.NewGuid();
        var request = new GetMemberBasicInfoBySyncGuidQuery(syncGuid);

        var expectedMember = new MemberBasicInfo
        {
            LoginId = "testuser",
            FirstName = "John",
            LastName = "Doe",
            MemberId = "MBR001"
        };

        _readRepositoryMock.Setup(repo => repo.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
            .ReturnsAsync(expectedMember);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedMember);

        _readRepositoryMock.Verify(repo => repo.GetAsync(
                It.Is<string>(sql => sql.Contains("FROM [User] U")),
                It.IsAny<CancellationToken>(),
                It.Is<object>(param => HasCorrectParameter(param, syncGuid)),
                null,
                "text"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenSyncGuidNotFound()
    {
        // Arrange
        var syncGuid = Guid.NewGuid();
        var request = new GetMemberBasicInfoBySyncGuidQuery(syncGuid);

        _readRepositoryMock.Setup(repo => repo.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
            .ReturnsAsync((MemberBasicInfo?)null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        _readRepositoryMock.Verify(repo => repo.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PassesCorrectParameters()
    {
        // Arrange
        var syncGuid = Guid.NewGuid();
        var query = new GetMemberBasicInfoBySyncGuidQuery(syncGuid);
        object capturedParameters = new DynamicParameters();

        _readRepositoryMock.Setup(r => r.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<object>(),
                null,
                "text"))
             .Callback<string, CancellationToken, object, object, string>(
                (sql, token, parameters, transaction, commandType) =>
                {
                    capturedParameters = parameters;
                })
            .ReturnsAsync(new MemberBasicInfo
            {
                LoginId = "test",
                FirstName = "Test",
                LastName = "User",
                MemberId = "MBR001"
            });

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedParameters.Should().NotBeNull();
        HasCorrectParameter(capturedParameters, syncGuid).Should().BeTrue();
    }

    private static bool HasCorrectParameter(object parameters, Guid expectedGuid)
    {
        if (parameters == null) return false;

        // Handle anonymous object
        var userSyncIdProperty = parameters.GetType().GetProperty("UserSyncId");
        if (userSyncIdProperty != null)
        {
            var value = userSyncIdProperty.GetValue(parameters);
            return value != null && value.Equals(expectedGuid);
        }

        return false;
    }
}

public static class StringExtensions
{
    public static string NormalizeWhitespace(this string input)
    {
        return Regex.Replace(input, @"\s+", " ").Trim();
    }
}