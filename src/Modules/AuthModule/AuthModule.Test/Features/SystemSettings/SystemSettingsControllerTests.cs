using System.Threading;
using FluentAssertions;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Moq;

namespace AuthModule.Test.Features.SystemSettings
{
    public class SystemSettingsServiceTests
    {
        private readonly Mock<IReadRepository<JustGo.Authentication.Infrastructure.SystemSettings.SystemSettings>> _repoMock;
        private readonly Mock<IUtilityService> _utilityMock;
        private readonly SystemSettingsService _service;

        public SystemSettingsServiceTests()
        {
            _repoMock = new Mock<IReadRepository<JustGo.Authentication.Infrastructure.SystemSettings.SystemSettings>>();
            _utilityMock = new Mock<IUtilityService>();

            var serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IReadRepository<JustGo.Authentication.Infrastructure.SystemSettings.SystemSettings>)))
                .Returns(_repoMock.Object);

            var lazyRepo = new LazyService<IReadRepository<JustGo.Authentication.Infrastructure.SystemSettings.SystemSettings>>(
                serviceProviderMock.Object);

            _service = new SystemSettingsService(lazyRepo, _utilityMock.Object);

        }



        [Fact]
        public async Task GetSystemSettingsByMultipleItemKey_String_ShouldReturnDecryptedValues_WhenRestricted()
        {
            var itemKeys = "SYSTEM.SITEADDRESS,INVOICE.ORGANISATIONNAME";
            var encryptedValue = "EncryptedValue";
            var decryptedValue = "DecryptedValue";
            var settings = new List<JustGo.Authentication.Infrastructure.SystemSettings.SystemSettings>
            {
                new JustGo.Authentication.Infrastructure.SystemSettings.SystemSettings { ItemKey = "SYSTEM.SITEADDRESS", Value = encryptedValue, Restricted = true },
                new JustGo.Authentication.Infrastructure.SystemSettings.SystemSettings { ItemKey = "INVOICE.ORGANISATIONNAME", Value = "PlainValue", Restricted = false }
            };

            _repoMock.Setup(r => r.GetListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), null, "text"))
                .ReturnsAsync((string qry, CancellationToken ct, object? dp, object? tr, string ctType) => settings.AsEnumerable());

            _utilityMock.Setup(u => u.DecryptData(encryptedValue)).Returns(decryptedValue);

            var result = await _service.GetSystemSettingsByMultipleItemKey(itemKeys, CancellationToken.None);

            result.Should().NotBeNull();
            result.Count.Should().Be(2);
            result.First(s => s.ItemKey == "SYSTEM.SITEADDRESS").Value.Should().Be(decryptedValue);
            result.First(s => s.ItemKey == "INVOICE.ORGANISATIONNAME").Value.Should().Be("PlainValue");
        }

        //getting faild on testing for collection
        [Fact]
        public async Task GetSystemSettingsByMultipleItemKey_List_ShouldReturnDecryptedValues_WhenRestricted()
        {
            var itemKeys = new List<string> { "SYSTEM.SITEADDRESS", "INVOICE.ORGANISATIONNAME" };
            
            var encryptedValue = "EncryptedValue";
            var decryptedValue = "DecryptedValue";
            var settings = new List<JustGo.Authentication.Infrastructure.SystemSettings.SystemSettings>
            {
                new JustGo.Authentication.Infrastructure.SystemSettings.SystemSettings { ItemKey = "SYSTEM.SITEADDRESS", Value = encryptedValue, Restricted = true },
                new JustGo.Authentication.Infrastructure.SystemSettings.SystemSettings { ItemKey = "INVOICE.ORGANISATIONNAME", Value = "PlainValue", Restricted = false }
            };

            _repoMock.Setup(r => r.GetListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), null, "text"))
                .ReturnsAsync((string qry, CancellationToken ct, object? dp, object? tr, string ctType) => settings.AsEnumerable());

            _utilityMock.Setup(u => u.DecryptData(encryptedValue)).Returns(decryptedValue);

            var result = await _service.GetSystemSettingsByMultipleItemKey(itemKeys, CancellationToken.None);

            result.Should().NotBeNull();
            result.Count.Should().Be(2);
            result.First(s => s.ItemKey == "SYSTEM.SITEADDRESS").Value.Should().Be(decryptedValue);
            result.First(s => s.ItemKey == "INVOICE.ORGANISATIONNAME").Value.Should().Be("PlainValue");
        }

        //[Fact]
        //public async Task GetSystemSettingsByMultipleItemKey_Array_ShouldReturnDecryptedValues_WhenRestricted()
        //{

        //    //var itemKeys = new[]
        //    //    {
        //    //        "SYSTEM.SITEADDRESS",
        //    //        "INVOICE.ORGANISATIONNAME"
        //    //    };
        //    string[] itemKeys =
        //    {
        //        "SYSTEM.SITEADDRESS",
        //        "INVOICE.ORGANISATIONNAME"
        //    };
        //    var encryptedValue = "EncryptedValue";
        //    var decryptedValue = "DecryptedValue";
        //    var settings = new List<JustGo.Authentication.Infrastructure.SystemSettings.SystemSettings>
        //    {
        //        new JustGo.Authentication.Infrastructure.SystemSettings.SystemSettings { ItemKey = "SYSTEM.SITEADDRESS", Value = encryptedValue, Restricted = true },
        //        new JustGo.Authentication.Infrastructure.SystemSettings.SystemSettings { ItemKey = "INVOICE.ORGANISATIONNAME", Value = "PlainValue", Restricted = false }
        //    };

        //    _repoMock.Setup(r => r.GetListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<object>(), null, "text"))
        //        .ReturnsAsync((string qry, CancellationToken ct, object? dp, object? tr, string ctType) => settings.AsEnumerable());

        //    _utilityMock.Setup(u => u.DecryptData(encryptedValue)).Returns(decryptedValue);

        //    var result = await _service.GetSystemSettingsByMultipleItemKey(itemKeys, CancellationToken.None);

        //    result.Should().NotBeNull();
        //    result.Count.Should().Be(2);
        //    result.First(s => s.ItemKey == "SYSTEM.SITEADDRESS").Value.Should().Be(decryptedValue);
        //    result.First(s => s.ItemKey == "INVOICE.ORGANISATIONNAME").Value.Should().Be("PlainValue");
        //}
    }
}
