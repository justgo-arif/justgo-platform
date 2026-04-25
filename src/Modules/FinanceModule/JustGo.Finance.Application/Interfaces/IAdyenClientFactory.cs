using Adyen;
using JustGo.Finance.Application.DTOs.Enums;

namespace JustGo.Finance.Application.Interfaces;

public interface IAdyenClientFactory
{
    Task<Client?> CreateClientAsync(AdyenKeyType keyType, CancellationToken cancellationToken = default);
    Task<string?> GetMerchantCodeAsync(CancellationToken cancellationToken = default);
}