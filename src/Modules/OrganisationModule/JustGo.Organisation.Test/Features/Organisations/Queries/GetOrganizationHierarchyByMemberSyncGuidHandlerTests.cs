using Dapper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Organisation.Application.Features.Organizations.Queries.GetOrganizationHierarchyByMemberSyncGuid;
using JustGo.Organisation.Domain.Entities;
using JustGo.Organisation.Test.Helper;
using Moq;

namespace JustGo.Organisation.Test.Features.Organisations.Queries
{
    public class GetOrganizationHierarchyByMemberSyncGuidHandlerTests
    {
        private readonly Mock<IReadRepository<HierarchyType>> _readRepositoryMock;
        private readonly GetOrganizationHierarchyByMemberSyncGuidHandler _handler;

        public GetOrganizationHierarchyByMemberSyncGuidHandlerTests()
        {
            _readRepositoryMock = new Mock<IReadRepository<HierarchyType>>();
            var lazyRepo = LazyServiceMockHelper.MockLazyService(_readRepositoryMock.Object);
            _handler = new GetOrganizationHierarchyByMemberSyncGuidHandler(lazyRepo);
        }

        [Fact]
        public async Task Handle_ShouldReturnHierarchyTypes_WhenUserExists()
        {
            // Arrange
            var syncGuid = Guid.NewGuid();
            var query = new GetOrganizationHierarchyByMemberSyncGuidQuery(syncGuid);

            var expectedResults = new List<HierarchyType>
            {
                new HierarchyType { Id = 1, HierarchyTypeName = "Region" },
                new HierarchyType { Id = 2, HierarchyTypeName = "Zone" }
            };

            _readRepositoryMock.Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    null,
                    "text"))
                .ReturnsAsync(expectedResults);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedResults);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenNoMatchesFound()
        {
            // Arrange
            var syncGuid = Guid.NewGuid();
            var query = new GetOrganizationHierarchyByMemberSyncGuidQuery(syncGuid);

            _readRepositoryMock.Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    null,
                    "text"))
                .ReturnsAsync(new List<HierarchyType>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_ShouldPassCorrectSqlAndParameter()
        {
            // Arrange
            var syncGuid = Guid.NewGuid();
            var query = new GetOrganizationHierarchyByMemberSyncGuidQuery(syncGuid);

            string capturedSql = null;
            DynamicParameters capturedParams = null;

            _readRepositoryMock.Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<DynamicParameters>(),
                    null,
                    "text"))
                .Callback<string, CancellationToken, object, object, string>((sql, ct, parameters, tx, cmd) =>
                {
                    capturedSql = sql;
                    capturedParams = parameters as DynamicParameters;
                })
                .ReturnsAsync(new List<HierarchyType>());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            capturedSql.Should().Contain("FROM [dbo].[HierarchyTypes] ht");
            capturedParams.Should().NotBeNull();
            capturedParams.Get<Guid>("@UserSyncId").Should().Be(syncGuid);
        }
    }
}
