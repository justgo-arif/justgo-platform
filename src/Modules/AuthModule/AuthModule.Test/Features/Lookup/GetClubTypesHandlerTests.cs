using AuthModule.Application.DTOs.Lookup;
using AuthModule.Application.Features.Lookup.Queries.GetClubTypes;
using AuthModule.Test.Helper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Moq;

namespace AuthModule.Test.Features.Lookup
{
    public class GetClubTypesHandlerTests
    {
        private readonly Mock<IReadRepository<SelectListItemDTO<string>>> _repoMock;
        private readonly GetClubTypesHandler _handler;

        public GetClubTypesHandlerTests()
        {
            _repoMock = new Mock<IReadRepository<SelectListItemDTO<string>>>();
            var lazyRepo = LazyServiceMockHelper.MockLazyService(_repoMock.Object);
            _handler = new GetClubTypesHandler(lazyRepo);
        }

        [Fact]
        public async Task Handle_ShouldReturnClubTypes_WhenResultsExist()
        {
            // Arrange
            var request = new GetClubTypesQuery(); // Empty query

            var expectedClubTypes = new List<SelectListItemDTO<string>>
            {
                new SelectListItemDTO<string> { Value = "Soccer", Text = "Soccer" },
                new SelectListItemDTO<string> { Value = "Basketball", Text = "Basketball" }
            };

            _repoMock.Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    null,
                    null,
                    "text"))
                .ReturnsAsync(expectedClubTypes);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedClubTypes);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenNoResultsExist()
        {
            // Arrange
            var request = new GetClubTypesQuery(); // Empty query

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
            var request = new GetClubTypesQuery(); // Empty query

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
            executedSql.Should().Contain("SELECT CD.ClubType [Value], CD.ClubType [Text]");
            executedSql.Should().Contain("WHERE ISNULL(CD.ClubType, '') <> ''");
        }
    }
}
