using System.Text.RegularExpressions;
using AuthModule.Application.DTOs.Lookup;
using AuthModule.Application.Features.Lookup.Queries.GetGender;
using AuthModule.Test.Helper;
using FluentAssertions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Moq;

namespace JustGo.MemberProfile.Test.Features.Lookup
{
    public class GetGenderHandlerTests
    {
        private readonly Mock<IReadRepository<SelectListItemDTO<string>>> _readRepositoryMock;
        private readonly GetGenderHandler _handler;

        public GetGenderHandlerTests()
        {
            _readRepositoryMock = new Mock<IReadRepository<SelectListItemDTO<string>>>();
            var lazyReadRepository = LazyServiceMockHelper.MockLazyService(_readRepositoryMock.Object);
            _handler = new GetGenderHandler(lazyReadRepository);
        }

        [Fact]
        public async Task Handle_ShouldReturnGenderList_WhenDataExists()
        {
            // Arrange
            var expectedGenders = new List<SelectListItemDTO<string>>
            {
                new SelectListItemDTO<string> { Text = "Male" },
                new SelectListItemDTO<string> { Text = "Female" },
                new SelectListItemDTO<string> { Text = "Other" }
            };

            _readRepositoryMock
                .Setup(r => r.GetListAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    null,
                    null,
                    "text"))
                .ReturnsAsync(expectedGenders);

            var request = new GetGenderQuery();

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().BeEquivalentTo(expectedGenders);
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

            var request = new GetGenderQuery();

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

            var request = new GetGenderQuery();

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            capturedSql.Should().NotBeNull();
            capturedSql.NormalizeSqlWhitespaceForGender().Should().Contain("SELECT genderSplit.value AS Text".NormalizeSqlWhitespaceForGender());
            capturedSql.Should().Contain("STRING_SPLIT");
            capturedSql.Should().Contain("JSON_VALUE");
            capturedSql.Should().Contain("ORGANISATION.GENDEROPTIONS");
        }
    }

    public static class StringExtensionsForGender
    {
        public static string NormalizeSqlWhitespaceForGender(this string input)
        {
            return Regex.Replace(input, @"\s+", " ").Trim();
        }
    }
}
