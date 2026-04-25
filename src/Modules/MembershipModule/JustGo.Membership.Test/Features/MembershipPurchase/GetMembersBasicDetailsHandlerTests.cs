using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Membership.Application.DTOs;
using JustGo.Membership.Application.Features.Memberships.Queries.GetMembersBasicDetailsQuery;
using JustGo.Membership.Test.Helper;
using Moq;

namespace JustGo.Membership.Test.Features.MembershipPurchase
{
    public class GetMembersBasicDetailsHandlerTests
    {
        private readonly Mock<IReadRepository<MemberDetailsDto>> _readRepositoryMock;
        private readonly GetMembersBasicDetailsHandler _handler;

        public GetMembersBasicDetailsHandlerTests()
        {
            _readRepositoryMock = new Mock<IReadRepository<MemberDetailsDto>>();
            var lazyReadRepository = LazyServiceMockHelper.MockLazyService(_readRepositoryMock.Object);
            _handler = new GetMembersBasicDetailsHandler(lazyReadRepository);
        }

        [Fact]
        public async Task Handle_ShouldReturnMemberDetailsList_WhenDataExists()
        {
            // Arrange
            var mockData = new List<MemberDetailsDto>
            {
                new MemberDetailsDto
                {
                    Userid = 1,
                    FirstName = "John",
                    LastName = "Doe",
                    EmailAddress = "john.doe@example.com",
                    Phone = "123456789",
                    MemberSyncGuid = "GUID123",
                    // Add other necessary properties here
                },
                new MemberDetailsDto
                {
                    Userid = 2,
                    FirstName = "Jane",
                    LastName = "Doe",
                    EmailAddress = "jane.doe@example.com",
                    Phone = "987654321",
                    MemberSyncGuid = "GUID124",
                    // Add other necessary properties here
                }
            };

            _readRepositoryMock.Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object>(),
                    null,
                    "text"))
                .ReturnsAsync(mockData);

            var query = new GetMembersBasicDetailsQuery(new List<int> { 101, 102 }); // Example MemberDocIds

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].FirstName.Should().Be("John");
            result[1].FirstName.Should().Be("Jane");
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenNoDataExists()
        {
            // Arrange
            _readRepositoryMock.Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object>(),
                    null,
                    "text"))
                .ReturnsAsync(new List<MemberDetailsDto>());

            var query = new GetMembersBasicDetailsQuery(new List<int> { 101, 102 }); // Example MemberDocIds

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }
}
