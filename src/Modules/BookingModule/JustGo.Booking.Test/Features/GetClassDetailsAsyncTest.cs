using JustGo.Booking.Application.DTOs.BookingClassesDTOs;
using JustGo.Booking.Application.Features.BookingClasses.Queries.ClassDetails;
using JustGo.Booking.Domain.Entities;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using MapsterMapper;
using Moq;
using System.Data;


namespace JustGo.Booking.Test.Features
{
    public class GetClassDetailsAsyncTest
    {
        private readonly Mock<IReadRepository<BookingSession>> _mockBookingSessionRepository;
        private readonly Mock<IHybridCacheService> _mockCache;
        private readonly Mock<IUtilityService> _mockUtilityService;
        private readonly GetClassDetailsHandler _handler;

        public GetClassDetailsAsyncTest()
        {
            var mockReadRepository = new Mock<IReadRepositoryFactory>();
            _mockBookingSessionRepository = new Mock<IReadRepository<BookingSession>>();
            _mockCache = new Mock<IHybridCacheService>();
            var mockMapper = new Mock<IMapper>();
            _mockUtilityService = new Mock<IUtilityService>();

            // Setup the repository factory to return our mocked BookingSession repository
            var lazyMock = MockLazyService(_mockBookingSessionRepository.Object);
            mockReadRepository
                .Setup(x => x.GetLazyRepository<BookingSession>())
                .Returns(lazyMock);

            _handler = new GetClassDetailsHandler(
                mockReadRepository.Object,
                _mockCache.Object,
                mockMapper.Object,
                _mockUtilityService.Object
            );
        }

        private LazyService<T> MockLazyService<T>(T instance) where T : class
        {
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(x => x.GetService(typeof(T))).Returns(instance);

            return new LazyService<T>(serviceProviderMock.Object);
        }

        private void SetupCacheMock(BookingClassDetailsDto? dtoToReturn)
        {
            _mockCache
                .Setup(x => x.GetOrSetAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<CancellationToken, Task<BookingClassDetailsDto?>>>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<string[]>(),
                    It.IsAny<CancellationToken>()
                ))
                .Returns<string, Func<CancellationToken, Task<BookingClassDetailsDto?>>, TimeSpan, string[], CancellationToken>(
                    async (key, factory, ttl, tags, ct) => dtoToReturn
                );
        }

        private void SetupBookingSessionRepositoryMock()
        {
            var bookingSession = new BookingSession
            {
                SessionId = 1,
                WaitlistOnly = false,
                AllSessionsFull = false,
                AvailableFullBookQty = 10,
                NoOfInvite = 0
            };

            _mockBookingSessionRepository
                .Setup(x => x.GetAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object>(),
                    It.IsAny<IDbTransaction>(),
                    It.IsAny<string>()
                ))
                .ReturnsAsync(bookingSession);
        }

        #region Valid Scenarios

        [Theory]
        [InlineData("550e8400-e29b-41d4-a716-446655440000", null)]
        [InlineData("650e8400-e29b-41d4-a716-446655440001", "invite123")]
        [InlineData("750e8400-e29b-41d4-a716-446655440002", "waitlist456")]
        public async Task Handle_WithValidSessionGuid_ShouldReturnClassDetails(string sessionGuidString, string? inviteId)
        {
            // Arrange
            var sessionGuid = Guid.Parse(sessionGuidString);
            var query = new GetClassDetailsQuery(sessionGuid, inviteId);
            var cancellationToken = CancellationToken.None;

            var expectedDto = CreateMockBookingClassDetailsDto();

            _mockUtilityService
                .Setup(x => x.GetCurrentUserPublic(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CurrentUser?)null);

            SetupCacheMock(expectedDto);
            SetupBookingSessionRepositoryMock();

            // Act
            var result = await _handler.Handle(query, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDto.Class.SessionGuid, result.Class.SessionGuid);
            Assert.Equal(expectedDto.Class.ClassName, result.Class.ClassName);
            _mockCache.Verify(
                x => x.GetOrSetAsync(
                    It.Is<string>(s => s.Contains(sessionGuidString)),
                    It.IsAny<Func<CancellationToken, Task<BookingClassDetailsDto?>>>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<string[]>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Theory]
        [InlineData(100)]
        [InlineData(500)]
        [InlineData(1000)]
        public async Task Handle_WithDifferentCapacities_ShouldReturnCorrectCapacity(int capacity)
        {
            // Arrange
            var sessionGuid = Guid.NewGuid();
            var query = new GetClassDetailsQuery(sessionGuid, null);
            var cancellationToken = CancellationToken.None;

            var dto = CreateMockBookingClassDetailsDto();
            dto.Class.Capacity = capacity;

            _mockUtilityService
                .Setup(x => x.GetCurrentUserPublic(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CurrentUser?)null);

            SetupCacheMock(dto);
            SetupBookingSessionRepositoryMock();

            // Act
            var result = await _handler.Handle(query, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(capacity, result.Class.Capacity);
        }

        [Theory]
        [InlineData("Draft")]
        [InlineData("Accepting Bookings")]
        [InlineData("Closed for Bookings")]
        [InlineData("Complete")]
        public async Task Handle_WithDifferentClassStates_ShouldReturnCorrectState(string classState)
        {
            // Arrange
            var sessionGuid = Guid.NewGuid();
            var query = new GetClassDetailsQuery(sessionGuid, null);
            var cancellationToken = CancellationToken.None;

            var dto = CreateMockBookingClassDetailsDto();
            dto.Class.ClassState = classState;

            _mockUtilityService
                .Setup(x => x.GetCurrentUserPublic(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CurrentUser?)null);

            SetupCacheMock(dto);
            SetupBookingSessionRepositoryMock();

            // Act
            var result = await _handler.Handle(query, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(classState, result.Class.ClassState);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Handle_WithOneOffAvailability_ShouldReturnCorrectAvailabilityStatus(bool isAvailable)
        {
            // Arrange
            var sessionGuid = Guid.NewGuid();
            var query = new GetClassDetailsQuery(sessionGuid, null);
            var cancellationToken = CancellationToken.None;

            var dto = CreateMockBookingClassDetailsDto();
            dto.Class.IsOneOffAvailable = isAvailable;

            _mockUtilityService
                .Setup(x => x.GetCurrentUserPublic(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CurrentUser?)null);

            SetupCacheMock(dto);
            SetupBookingSessionRepositoryMock();

            // Act
            var result = await _handler.Handle(query, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(isAvailable, result.Class.IsOneOffAvailable);
        }

        [Theory]
        [InlineData(29.99, 49.99, 79.99)]
        [InlineData(15.50, 30.00, 45.50)]
        [InlineData(100.00, 200.00, 300.00)]
        public async Task Handle_WithDifferentPrices_ShouldReturnCorrectPrices(
            decimal oneOffPrice, decimal monthlyPrice, decimal paygPrice)
        {
            // Arrange
            var sessionGuid = Guid.NewGuid();
            var query = new GetClassDetailsQuery(sessionGuid, null);
            var cancellationToken = CancellationToken.None;

            var dto = CreateMockBookingClassDetailsDto();
            dto.Class.OneOffPrice = oneOffPrice;
            dto.Class.MonthlyPrice = monthlyPrice;
            dto.Class.PaygPrice = paygPrice;

            _mockUtilityService
                .Setup(x => x.GetCurrentUserPublic(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CurrentUser?)null);

            SetupCacheMock(dto);
            SetupBookingSessionRepositoryMock();

            // Act
            var result = await _handler.Handle(query, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(oneOffPrice, result.Class.OneOffPrice);
            Assert.Equal(monthlyPrice, result.Class.MonthlyPrice);
            Assert.Equal(paygPrice, result.Class.PaygPrice);
        }

        [Theory]
        [InlineData("M,F")]
        [InlineData("M")]
        [InlineData("F")]
        public async Task Handle_WithDifferentGenderRestrictions_ShouldReturnCorrectGender(string genderString)
        {
            // Arrange
            var sessionGuid = Guid.NewGuid();
            var query = new GetClassDetailsQuery(sessionGuid, null);
            var cancellationToken = CancellationToken.None;

            var genderArray = genderString.Split(',');
            var dto = CreateMockBookingClassDetailsDto();
            dto.Class.Gender = genderArray;

            _mockUtilityService
                .Setup(x => x.GetCurrentUserPublic(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CurrentUser?)null);

            SetupCacheMock(dto);
            SetupBookingSessionRepositoryMock();

            // Act
            var result = await _handler.Handle(query, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Class.Gender);
            Assert.Equal(genderArray.Length, result.Class.Gender.Length);
        }

        [Theory]
        [InlineData(5, 65)]
        [InlineData(18, 30)]
        [InlineData(0, 100)]
        public async Task Handle_WithDifferentAgeGroups_ShouldReturnCorrectAgeRange(int minAge, int maxAge)
        {
            // Arrange
            var sessionGuid = Guid.NewGuid();
            var query = new GetClassDetailsQuery(sessionGuid, null);
            var cancellationToken = CancellationToken.None;

            var dto = CreateMockBookingClassDetailsDto();
            dto.Class.MinAge = minAge;
            dto.Class.MaxAge = maxAge;

            _mockUtilityService
                .Setup(x => x.GetCurrentUserPublic(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CurrentUser?)null);

            SetupCacheMock(dto);
            SetupBookingSessionRepositoryMock();

            // Act
            var result = await _handler.Handle(query, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(minAge, result.Class.MinAge);
            Assert.Equal(maxAge, result.Class.MaxAge);
        }

        #endregion

        #region Null/Empty Scenarios

        [Fact]
        public async Task Handle_WhenCacheReturnsNull_ShouldReturnNull()
        {
            // Arrange
            var sessionGuid = Guid.NewGuid();
            var query = new GetClassDetailsQuery(sessionGuid, null);
            var cancellationToken = CancellationToken.None;

            _mockUtilityService
                .Setup(x => x.GetCurrentUserPublic(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CurrentUser?)null);

            SetupCacheMock(null);
            SetupBookingSessionRepositoryMock();

            // Act
            var result = await _handler.Handle(query, cancellationToken);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_WithEmptyCoaches_ShouldReturnEmptyCoachesList()
        {
            // Arrange
            var sessionGuid = Guid.NewGuid();
            var query = new GetClassDetailsQuery(sessionGuid, null);
            var cancellationToken = CancellationToken.None;

            var dto = CreateMockBookingClassDetailsDto();
            dto.Coaches = new List<SessionCoachDto>();

            _mockUtilityService
                .Setup(x => x.GetCurrentUserPublic(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CurrentUser?)null);

            SetupCacheMock(dto);
            SetupBookingSessionRepositoryMock();

            // Act
            var result = await _handler.Handle(query, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Coaches);
            Assert.Empty(result.Coaches);
        }

        [Fact]
        public async Task Handle_WithEmptyOccurrences_ShouldReturnEmptyOccurrencesList()
        {
            // Arrange
            var sessionGuid = Guid.NewGuid();
            var query = new GetClassDetailsQuery(sessionGuid, null);
            var cancellationToken = CancellationToken.None;

            var dto = CreateMockBookingClassDetailsDto();
            dto.Occurrences = new List<SessionOccurrenceDto>();

            _mockUtilityService
                .Setup(x => x.GetCurrentUserPublic(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CurrentUser?)null);

            SetupCacheMock(dto);
            SetupBookingSessionRepositoryMock();

            // Act
            var result = await _handler.Handle(query, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Occurrences);
            Assert.Empty(result.Occurrences);
        }

        #endregion

        #region Cache Key and TTL Verification

        [Fact]
        public async Task Handle_ShouldUseCacheWithCorrectKeyFormat()
        {
            // Arrange
            var sessionGuid = Guid.NewGuid();
            var query = new GetClassDetailsQuery(sessionGuid, null);
            var cancellationToken = CancellationToken.None;
            var expectedCacheKey = $"justgobooking:class-details:{sessionGuid}";

            var dto = CreateMockBookingClassDetailsDto();

            _mockUtilityService
                .Setup(x => x.GetCurrentUserPublic(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CurrentUser?)null);

            SetupCacheMock(dto);
            SetupBookingSessionRepositoryMock();

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _mockCache.Verify(
                x => x.GetOrSetAsync(
                    expectedCacheKey,
                    It.IsAny<Func<CancellationToken, Task<BookingClassDetailsDto?>>>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<string[]>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task Handle_ShouldUseCacheTtlOf10Minutes()
        {
            // Arrange
            var sessionGuid = Guid.NewGuid();
            var query = new GetClassDetailsQuery(sessionGuid, null);
            var cancellationToken = CancellationToken.None;
            var expectedTtl = TimeSpan.FromMinutes(10);

            var dto = CreateMockBookingClassDetailsDto();

            _mockUtilityService
                .Setup(x => x.GetCurrentUserPublic(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CurrentUser?)null);

            SetupCacheMock(dto);
            SetupBookingSessionRepositoryMock();

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _mockCache.Verify(
                x => x.GetOrSetAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<CancellationToken, Task<BookingClassDetailsDto?>>>(),
                    expectedTtl,
                    It.IsAny<string[]>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        #endregion

        #region Venue Information

        [Fact]
        public async Task Handle_ShouldReturnVenueDetails()
        {
            // Arrange
            var sessionGuid = Guid.NewGuid();
            var query = new GetClassDetailsQuery(sessionGuid, null);
            var cancellationToken = CancellationToken.None;

            var dto = CreateMockBookingClassDetailsDto();

            _mockUtilityService
                .Setup(x => x.GetCurrentUserPublic(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CurrentUser?)null);

            SetupCacheMock(dto);
            SetupBookingSessionRepositoryMock();

            // Act
            var result = await _handler.Handle(query, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Venue);
            Assert.NotNull(result.Venue.Name);
            Assert.NotEmpty(result.Venue.Name);
        }

        #endregion

        #region Schedule Information

        [Fact]
        public async Task Handle_ShouldReturnScheduleInformation()
        {
            // Arrange
            var sessionGuid = Guid.NewGuid();
            var query = new GetClassDetailsQuery(sessionGuid, null);
            var cancellationToken = CancellationToken.None;

            var dto = CreateMockBookingClassDetailsDto();
            dto.ScheduleInfo = new List<ScheduleInfoDto>
            {
                new ScheduleInfoDto { Day = "Monday", StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0) },
                new ScheduleInfoDto { Day = "Wednesday", StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0) }
            };

            _mockUtilityService
                .Setup(x => x.GetCurrentUserPublic(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CurrentUser?)null);

            SetupCacheMock(dto);
            SetupBookingSessionRepositoryMock();

            // Act
            var result = await _handler.Handle(query, cancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ScheduleInfo);
            Assert.Equal(2, result.ScheduleInfo.Count);
        }

        #endregion

        #region Helper Methods

        private static BookingClassDetailsDto CreateMockBookingClassDetailsDto()
        {
            return new BookingClassDetailsDto
            {
                Class = new BookingSessionDto
                {
                    SessionId = 1,
                    SessionName = "Test Session",
                    SessionGuid = Guid.NewGuid(),
                    Capacity = 30,
                    ClassId = 1,
                    ClassName = "Test Class",
                    ClassGuid = Guid.NewGuid(),
                    OwningEntitySyncGuid = Guid.NewGuid(),
                    ClassState = "Accepting Bookings",
                    Description = "Test Description",
                    BookingStartDate = DateTime.UtcNow.AddDays(-7),
                    BookingEndDate = DateTime.UtcNow.AddDays(30),
                    CategoryId = 1,
                    CategoryName = "Test Category",
                    AgeGroupId = 1,
                    AgeGroupName = "Adults",
                    MinAge = 18,
                    MaxAge = 65,
                    Gender = new[] { "M", "F" },
                    TrialLimit = 2,
                    ColorName = "Blue",
                    ColorCode = "#0000FF",
                    ClassImages = new[] { "image1.jpg", "image2.jpg" },
                    IsOneOffAvailable = true,
                    OneOffPrice = 29.99m,
                    IsMonthlyAvailable = true,
                    IsDynamicAvailable = false,
                    IsHourlyPricingAvailable = false,
                    MonthlyPrice = 99.99m,
                    HourlyPrice = 0,
                    IsPaygAvailable = true,
                    PaygPrice = 15.00m,
                    IsTrialAvailable = true,
                    TrialPrice = 0,
                    IsWaitable = false
                },
                ScheduleInfo =
                [
                    new ScheduleInfoDto
                        { Day = "Monday", StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0) }
                ],
                Venue = new SessionVenueDto
                {
                    Name = "Test Venue",
                    Address1 = "123 Main Street",
                    Address2 = "Suite 100",
                    County = "Test County",
                    Postcode = "AB12 3CD",
                    Region = "Test Region",
                    Country = "Test Country",
                    Latlng = "51.5074,-0.1278"
                },
                Coaches =
                [
                    new SessionCoachDto
                    {
                        MemberId = "123",
                        CoachName = "John Doe",
                        Role = "Head Coach",
                        ImageUrl = "https://example.com/image.jpg"
                    }
                ],
                Occurrences =
                [
                    new SessionOccurrenceDto
                    {
                        StartDate = DateTime.UtcNow.AddDays(1),
                        EndDate = DateTime.UtcNow.AddDays(1).AddHours(1),
                        IsHoliday = false
                    }
                ],
                HourlyPricingChartDto = []
            };
        }

        #endregion
    }
}
