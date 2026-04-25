using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Application.Features.UserEmergencyContacts.Queries.GetRelationship;
using JustGo.MemberProfile.Test.Helper;
using Moq;

namespace JustGo.MemberProfile.Test.Features.UserEmergencyContacts.Queries
{
    public class GetUserRelationshipHandlerTests
    {
        private readonly Mock<IReadRepository<UserRelationshipDto>> _readRepositoryMock;
        private readonly GetRelationshipHandler _handler;

        public GetUserRelationshipHandlerTests()
        {
            _readRepositoryMock = new Mock<IReadRepository<UserRelationshipDto>>();
            var lazyReadRepository = LazyServiceMockHelper.MockLazyService(_readRepositoryMock.Object);
            _handler = new GetRelationshipHandler(lazyReadRepository);
        }

        [Fact]
        public async Task Handle_ShouldReturnRelationshipList_WhenDataExists()
        {
            // Arrange
            var mockData = new List<UserRelationshipDto>
            {
                new UserRelationshipDto { RowId = 1, Name = "Father" },
                new UserRelationshipDto { RowId = 2, Name = "Mother" }
            };

            _readRepositoryMock.Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    null,
                    null,
                    "text"))
                .ReturnsAsync(mockData);

            // Act
            var result = await _handler.Handle(new GetRelationshipQuery(), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Name.Should().Be("Father");
            result[1].Name.Should().Be("Mother");
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenNoDataExists()
        {
            // Arrange
            _readRepositoryMock.Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    null,
                    null,
                    "text"))
                .ReturnsAsync(new List<UserRelationshipDto>());

            // Act
            var result = await _handler.Handle(new GetRelationshipQuery(), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }
}
