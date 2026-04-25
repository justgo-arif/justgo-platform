using System.Text.RegularExpressions;
using AuthModule.Application.DTOs.Lookup;
using AuthModule.Application.Features.Lookup.Queries.GetCountys;
using AuthModule.Test.Helper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Moq;

namespace JustGo.MemberProfile.Test.Features.Lookup
{
    public class GetCountyHandlerTests
    {
        private readonly Mock<IReadRepository<SelectListItemDTO<string>>> _readRepositoryMock;
        private readonly GetCountysHandler _handler;

        public GetCountyHandlerTests()
        {
            _readRepositoryMock = new Mock<IReadRepository<SelectListItemDTO<string>>>();
            var lazyReadRepository = LazyServiceMockHelper.MockLazyService(_readRepositoryMock.Object);
            _handler = new GetCountysHandler(lazyReadRepository);
        }

        [Fact]
        public async Task Handle_ShouldReturnCountyList_WhenDataExists()
        {
            // Arrange
            var expectedCounties = new List<SelectListItemDTO<string>>
            {
                new SelectListItemDTO<string> { Id = "1", Text = "California" },
                new SelectListItemDTO<string> { Id = "2", Text = "Texas" }
            };

            _readRepositoryMock
                .Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    null,
                    null,
                    "text"))
                .ReturnsAsync(expectedCounties);

            var request = new GetCountysQuery();

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(expectedCounties);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenNoDataExists()
        {
            // Arrange
            _readRepositoryMock
                .Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    null,
                    null,
                    "text"))
                .ReturnsAsync(new List<SelectListItemDTO<string>>());

            var request = new GetCountysQuery();

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_ShouldExecuteCorrectSql()
        {
            // Arrange
            string capturedSql = null;

            _readRepositoryMock
                .Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    null,
                    null,
                    "text"))
                .Callback<string, CancellationToken, object, object, string>((sql, ct, param, trans, type) =>
                {
                    capturedSql = sql;
                })
                .ReturnsAsync(new List<SelectListItemDTO<string>>());

            var request = new GetCountysQuery();

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            capturedSql.Should().NotBeNull();
            capturedSql.NormalizeWhitespace().Should().Contain("SELECT RowId AS Id, County AS Text".NormalizeWhitespace());
            capturedSql.Should().Contain("SELECT dbo.GetLookupTableQuery('County')");
        }
    }

    public static class StringExtensionsForCounty
    {
        public static string NormalizeSqlWhitespaceForCounty(this string input)
        {
            return Regex.Replace(input, @"\s+", " ").Trim();
        }
    }
}
