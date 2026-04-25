using System.Text.Json;
using Adyen;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.Interfaces;
using JustGo.Finance.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Infrastructure.AdyenClientConfiguration
{
    public class AdyenClientFactory : IAdyenClientFactory
    {
        private readonly IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;

        public AdyenClientFactory(IMediator mediator, ISystemSettingsService systemSettingsService)
        {
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }

        public async Task<Client?> CreateClientAsync(AdyenKeyType keyType, CancellationToken cancellationToken = default)
        {
            var settingApiJson = await _systemSettingsService.GetSystemSettingsByItemKey(
                "SYSTEM.PAYMENT.ADYENAPIKEY", cancellationToken);

            if (String.IsNullOrEmpty(settingApiJson)) return null;

            var settingApiKeys = JsonSerializer.Deserialize<AdyenApiKeySettings>(settingApiJson);

            if (settingApiKeys == null)
                throw new InvalidOperationException("Could not deserialize Adyen API key settings.");

            string apiKey = keyType switch
            {
                AdyenKeyType.LegalEntity => settingApiKeys.LegalEntityApiKey!,
                AdyenKeyType.Checkout => settingApiKeys.CheckoutApiKey!,
                AdyenKeyType.BalancePlatform => settingApiKeys.BalancePlatformApiKey!,
                _ => throw new ArgumentOutOfRangeException(nameof(keyType))
            };

            var config = new Config
            {
                XApiKey = apiKey,
                Environment = settingApiKeys.Environment == "test"
                    ? Adyen.Model.Environment.Test
                    : Adyen.Model.Environment.Live
            };

            if (settingApiKeys.Environment != "test")
            {
                config.LiveEndpointUrlPrefix = settingApiKeys.EnpointPrefix;
            }

            return new Client(config);
        }

        public async Task<string?> GetMerchantCodeAsync(CancellationToken cancellationToken = default)
        {
            var settingApiJson = await _systemSettingsService.GetSystemSettingsByItemKey(
                "SYSTEM.PAYMENT.ADYENAPIKEY", cancellationToken);

            if (String.IsNullOrEmpty(settingApiJson)) return null;

            var settingApiKeys = JsonSerializer.Deserialize<AdyenApiKeySettings>(settingApiJson);

            if (settingApiKeys == null)
                throw new InvalidOperationException("Could not deserialize Adyen API key settings.");

            return settingApiKeys.MerchantCode;
        }


    }
}
