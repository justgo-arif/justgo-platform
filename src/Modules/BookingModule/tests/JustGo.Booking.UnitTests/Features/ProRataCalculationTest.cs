using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Booking.Application.DTOs.ClassManagementDtos;
using JustGo.Booking.Application.Features.ClassManagement.Queries.ProRataCalculation;
using Moq;
using System.Data;

namespace JustGo.Booking.Test.Features
{
    public class ProRataCalculationTest
    {
        private readonly Mock<IReadRepository<object>> _readRepository;
        private readonly ProRataCalculationHandler _handler;

        public ProRataCalculationTest()
        {
            var readRepositoryFactory = new Mock<IReadRepositoryFactory>();
            _readRepository = new Mock<IReadRepository<object>>();

            readRepositoryFactory
                .Setup(x => x.GetLazyRepository<object>())
                .Returns(MockLazyService(_readRepository.Object));

            _handler = new ProRataCalculationHandler(readRepositoryFactory.Object);
        }

        [Fact]
        public async Task Handle_WithValidRequest_ReturnsDiscount()
        {
            var classProductId = 123;
            var startDate = new DateTime(2024, 10, 15, 0, 0, 0, DateTimeKind.Utc);
            var expectedDiscount = 12.5m;
            var cancellationToken = CancellationToken.None;
            DynamicParameters? capturedParameters = null;

            _readRepository
                .Setup(x => x.GetSingleAsync<decimal>(
                    It.Is<string>(q => q == "CalculateProRataDiscountByClassProduct"),
                    It.IsAny<object?>(),
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<CancellationToken>(),
                    QueryType.StoredProcedure))
                .Callback<string, object?, IDbTransaction?, CancellationToken, string>((_, parameters, _, _, _) =>
                {
                    capturedParameters = parameters as DynamicParameters;
                })
                .ReturnsAsync(expectedDiscount);

            var query = new ProRataCalculationQuery(new ProRataCalculationRequestDto
            {
                ClassProductId = classProductId,
                StartDate = startDate
            });

            var result = await _handler.Handle(query, cancellationToken);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            var discountProperty = result.Value!.GetType().GetProperty("ProRataDiscount");
            Assert.NotNull(discountProperty);
            var actualDiscount = Assert.IsType<decimal>(discountProperty!.GetValue(result.Value));
            Assert.Equal(expectedDiscount, actualDiscount);

            Assert.NotNull(capturedParameters);
            Assert.True(TryGetParameter(capturedParameters!, "ClassProductDocId", out int classId)
                || TryGetParameter(capturedParameters!, "@ClassProductDocId", out classId));
            Assert.True(TryGetParameter(capturedParameters!, "StartDate", out DateTime occurrenceDate)
                || TryGetParameter(capturedParameters!, "@StartDate", out occurrenceDate));
            Assert.Equal(classProductId, classId);
            Assert.Equal(startDate, occurrenceDate);

            _readRepository.Verify(x => x.GetSingleAsync<decimal>(
                It.Is<string>(q => q == "CalculateProRataDiscountByClassProduct"),
                It.IsAny<object?>(),
                It.IsAny<IDbTransaction>(),
                cancellationToken,
                QueryType.StoredProcedure),
                Times.Once);
        }

        private static LazyService<T> MockLazyService<T>(T instance) where T : class
        {
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(typeof(T))).Returns(instance);
            return new LazyService<T>(serviceProviderMock.Object);
        }

        private static bool TryGetParameter<T>(DynamicParameters parameters, string name, out T value)
        {
            if (parameters.ParameterNames.Contains(name))
            {
                value = parameters.Get<T>(name);
                return true;
            }

            value = default!;
            return false;
        }
    }
}
