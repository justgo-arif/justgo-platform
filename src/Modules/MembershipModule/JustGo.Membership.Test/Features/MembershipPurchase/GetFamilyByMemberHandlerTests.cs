using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Membership.Application.DTOs;
using JustGo.Membership.Application.Features.Memberships.Queries.GetFamilyByMemberDocId;
using JustGo.Membership.Test.Helper;
using Moq;

namespace JustGo.Membership.Test.Features.MembershipPurchase
{
    public class GetFamilyByMemberHandlerTests
    {
        private readonly Mock<IReadRepository<FamilyDetailsDto>> _readRepositoryMock;
        private readonly GetFamilyByMemberHandler _handler;

        public GetFamilyByMemberHandlerTests()
        {
            _readRepositoryMock = new Mock<IReadRepository<FamilyDetailsDto>>();
            var lazyReadRepository = LazyServiceMockHelper.MockLazyService(_readRepositoryMock.Object);
            _handler = new GetFamilyByMemberHandler(lazyReadRepository);
        }

        [Fact]
        public async Task Handle_ShouldReturnFamilyDetails_WhenDataExists()
        {
            // Arrange
            var mockFamilyDetails = new FamilyDetailsDto
            {
                DocId = 1,
                Reference = "Ref123",
                Familyname = "Doe",
                Members = "1,2,3"
            };

            _readRepositoryMock.Setup(r => r.GetAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object>(),
                    null,
                    "text"))
                .ReturnsAsync(mockFamilyDetails);

            var query = new GetFamilyByMemberQuery(101); // Example member DocId

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.DocId.Should().Be(1);
            result.Reference.Should().Be("Ref123");
            result.Familyname.Should().Be("Doe");
            result.Members.Should().Be("1,2,3");
        }

        [Fact]
        public async Task Handle_ShouldReturnNull_WhenNoDataExists()
        {
            // Arrange
            _readRepositoryMock.Setup(r => r.GetAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object>(),
                    null,
                    "text"))
                .ReturnsAsync((FamilyDetailsDto?)null);

            var query = new GetFamilyByMemberQuery(101); // Example member DocId

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }
    }
}
