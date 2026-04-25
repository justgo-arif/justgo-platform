using AuthModule.Application.DTOs.Lookup;
using AuthModule.Application.Features.Lookup.Queries.GetRegions;
using AuthModule.Test.Helper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Moq;

namespace AuthModule.Test.Features.Lookup
{
    public class GetRegionsHandlerTests
    {
        private readonly Mock<IReadRepository<SelectListItemDTO<string>>> _repoMock;
        private readonly GetRegionsHandler _handler;

        public GetRegionsHandlerTests()
        {
            _repoMock = new Mock<IReadRepository<SelectListItemDTO<string>>>();
            var lazyRepo = LazyServiceMockHelper.MockLazyService(_repoMock.Object);
            _handler = new GetRegionsHandler(lazyRepo);
        }

        [Fact]
        public async Task Handle_ShouldReturnRegions_WhenResultsExist()
        {
            // Arrange
            var request = new GetRegionsQuery(); // Empty query

            var expectedRegions = new List<SelectListItemDTO<string>>
            {
                new SelectListItemDTO<string> { Value = "Region Alpha", Text = "Region Alpha" },
                new SelectListItemDTO<string> { Value = "Region Beta", Text = "Region Beta" }
            };

            _repoMock.Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    null,
                    null,
                    "text"))
                .ReturnsAsync(expectedRegions);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedRegions);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenNoResultsExist()
        {
            // Arrange
            var request = new GetRegionsQuery(); // Empty query

            _repoMock.Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    null,
                    null,
                    "text"))
                .ReturnsAsync(new List<SelectListItemDTO<string>>());

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_ShouldUseCorrectSql()
        {
            // Arrange
            var request = new GetRegionsQuery(); // Empty query

            string executedSql = null;

            _repoMock.Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    null,
                    null,
                    "text"))
                .Callback<string, CancellationToken, object, object, string>((sql, _, _, _, _) =>
                {
                    executedSql = sql;
                })
                .ReturnsAsync(new List<SelectListItemDTO<string>>());

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            executedSql.Should().NotBeNullOrEmpty();
            executedSql.Should().Contain("SELECT [County] [Value], [County] [Text]");
            executedSql.Should().Contain("WHERE ISNULL(County, '''') != ''''");
            executedSql.Should().Contain("ORDER BY County ASC");
        }
    }
}
