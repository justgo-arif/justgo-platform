using FluentAssertions;
using JustGo.Authentication.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.MemberProfile.Application.DTOs;
using JustGo.MemberProfile.Application.Features.AddressPickers.Queries.GetAddressesByPostCode;
using JustGo.MemberProfile.Test.Helper;
using Microsoft.AspNetCore.Http;
using Moq;

namespace JustGo.MemberProfile.Test.Features.AddressPickers
{
    public class GetAddressesByPostCodeHandlerTests
    {
        private readonly Mock<IReadRepository<AddressDto>> _addressRepoMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<ISystemSettingsService> _systemSettingsMock;
        private readonly GetAddressesByPostCodeHandler _handler;

        public GetAddressesByPostCodeHandlerTests()
        {
            _addressRepoMock = new Mock<IReadRepository<AddressDto>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _systemSettingsMock = new Mock<ISystemSettingsService>();

            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(new DefaultHttpContext());

            var lazyAddressRepo = LazyServiceMockHelper.MockLazyService(_addressRepoMock.Object);

            _handler = new GetAddressesByPostCodeHandler(lazyAddressRepo, _systemSettingsMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Call_GetAddressesByPostCode_When_Mode_Is_PostCodeFinder()
        {
            // Arrange
            var expected = new List<AddressDto> { new AddressDto { Address1 = "ABC Road", Town = "Dhaka" } };

            _systemSettingsMock
                .Setup(s => s.GetSystemSettingsByItemKey("CLUBPLUS.HOSTSYSTEMID", It.IsAny<CancellationToken>()))
                .ReturnsAsync("7035383417Internal");

            _addressRepoMock
                .Setup(r => r.GetListAsync("GetAddressesByPostCode", It.IsAny<CancellationToken>(), It.IsAny<object>(), null, "sp"))
                .ReturnsAsync(expected);

            var query = new GetAddressesByPostCodeQuery
            {
                Mode = "PostCodeFinder",
                CountryName = "Bangladesh",
                Keyword = "1212"
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(expected);

            _addressRepoMock.Verify(r =>
                r.GetListAsync("GetAddressesByPostCode", It.IsAny<CancellationToken>(), It.IsAny<object>(), null, "sp"),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Call_GetAddressesByKeyword_When_Mode_Is_AddressFinder()
        {
            // Arrange
            var expected = new List<AddressDto> { new AddressDto { Address1 = "XYZ Street", Town = "Chittagong" } };

            _systemSettingsMock
                .Setup(s => s.GetSystemSettingsByItemKey("CLUBPLUS.HOSTSYSTEMID", It.IsAny<CancellationToken>()))
                .ReturnsAsync("Host123");

            _addressRepoMock
                .Setup(r => r.GetListAsync("GetAddressesByKeyword", It.IsAny<CancellationToken>(), It.IsAny<object>(), null, "sp"))
                .ReturnsAsync(expected);

            var query = new GetAddressesByPostCodeQuery
            {
                Mode = "AddressFinder",
                CountryName = "Bangladesh",
                Keyword = "Banani"
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().BeEquivalentTo(expected);

            _addressRepoMock.Verify(r =>
                r.GetListAsync("GetAddressesByKeyword", It.IsAny<CancellationToken>(), It.IsAny<object>(), null, "sp"),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Return_EmptyList_When_Repository_Returns_Empty()
        {
            // Arrange
            _systemSettingsMock
                .Setup(s => s.GetSystemSettingsByItemKey("CLUBPLUS.HOSTSYSTEMID", It.IsAny<CancellationToken>()))
                .ReturnsAsync("Host123");

            _addressRepoMock
                .Setup(r => r.GetListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), null, "sp"))
                .ReturnsAsync(new List<AddressDto>());

            var query = new GetAddressesByPostCodeQuery
            {
                Mode = "PostCodeFinder",
                CountryName = "Bangladesh",
                Keyword = "0000"
            };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
    }
}
