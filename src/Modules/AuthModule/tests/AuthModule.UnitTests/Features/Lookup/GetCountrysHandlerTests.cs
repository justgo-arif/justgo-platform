using AuthModule.Application.DTOs.Lookup;
using AuthModule.Application.Features.Lookup.Queries.GetCountrys;
using AuthModule.Test.Helper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Moq;

namespace JustGo.MemberProfile.Test.Features.Lookup
{
    public class GetCountrysHandlerTests
    {
        private readonly Mock<IReadRepository<SelectListItemDTO<string>>> _readRepositoryMock;
        private readonly GetCountrysHandler _handler;

        public GetCountrysHandlerTests()
        {
            _readRepositoryMock = new Mock<IReadRepository<SelectListItemDTO<string>>>();
            var lazyReadRepository = LazyServiceMockHelper.MockLazyService(_readRepositoryMock.Object);
            _handler = new GetCountrysHandler(lazyReadRepository);
        }

        [Fact]
        public async Task Handle_ShouldReturnCountryList_WhenDataExists()
        {
            // Arrange
            var expectedCountries = new List<SelectListItemDTO<string>>
            {
                new SelectListItemDTO<string> { Id = "1", Text = "Bangladesh", Value = "BD" },
                new SelectListItemDTO<string> { Id = "2", Text = "India", Value = "IN" }
            };

            _readRepositoryMock
                .Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    null,
                    null,
                    "text"))
                .ReturnsAsync(expectedCountries);

            var request = new GetCountrysQuery();

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(expectedCountries);
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

            var request = new GetCountrysQuery();

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

            var request = new GetCountrysQuery();

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            capturedSql.Should().NotBeNull();
            capturedSql.NormalizeWhitespace().Should().Contain("SELECT RowId AS Id, Country AS Text, CountryCode AS Value".NormalizeWhitespace());
            capturedSql.Should().Contain("SELECT dbo.GetLookupTableQuery('Country')");

        }
    }

    public static class StringExtensions
    {
        public static string NormalizeWhitespace(this string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, @"\s+", " ").Trim();
        }
    }
}
