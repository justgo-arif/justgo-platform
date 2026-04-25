using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.Common.Constants;
using JustGo.Finance.Application.Common.Helpers;
using JustGo.Finance.Application.DTOs.PaymentConsoleDtos;
using JustGo.Finance.Application.Features.PaymentAccount.Queries.GetAdyenPaymentAccountDetails;
using JustGo.Finance.Application.Features.PaymentAccount.Queries.GetSavedAdyenAccountDetails;
using JustGo.Finance.Application.Features.PaymentAccount.Queries.GetStripeMerchantProfile;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentConsole.Queries.GetMerchantPaymentEligibility;

public class GetMerchantPaymentEligibilityHandler : IRequestHandler<GetMerchantPaymentEligibilityQuery, MerchantPaymentEligibilityDto>
{
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly IMediator _mediator;
    private readonly LazyService<IReadRepository<string>> _readRepository;


    public GetMerchantPaymentEligibilityHandler(
        ISystemSettingsService systemSettingsService,
        IMediator mediator,
        LazyService<IReadRepository<string>> readRepository)
    {
        _systemSettingsService = systemSettingsService;
        _mediator = mediator;
        _readRepository = readRepository;
    }
    public async Task<MerchantPaymentEligibilityDto> Handle(GetMerchantPaymentEligibilityQuery request, CancellationToken cancellationToken)
    {

        var enableAdyenPayment = await _systemSettingsService.GetSystemSettingsByItemKey(
                "SYSTEM.PAYMENT.EnableAdyenPayment", cancellationToken);

        bool isAdyenEnabled = enableAdyenPayment != null
            && bool.TryParse(enableAdyenPayment, out var isEnabled)
            && isEnabled;


        if (isAdyenEnabled)
        {
            var adyenAccount = await _mediator.Send(new GetAdyenPaymentAccountDetailsQuery(request.MerchantId));

            if(adyenAccount == null || adyenAccount.Problems?.Count > 0 || !adyenAccount.IsPaymentEnabled)
            {
                return new MerchantPaymentEligibilityDto
                {
                    IsEligible = false,
                    Reason = "Adyen account not found for the merchant."
                };
            }
        }
        else
        {
            var merchantDocId = await _readRepository.Value
                .GetSingleAsync(SqlQueries.SelectDocIdBySyncGuid, cancellationToken, QueryHelpers.GetGuidParams(request.MerchantId), null, "text");

            if(merchantDocId == null)
            {
                return new MerchantPaymentEligibilityDto
                {
                    IsEligible = false,
                    Reason = "Merchant not found."
                };
            }

            var stripeAccount = await _mediator.Send(new GetStripeMerchantProfileQuery((int)merchantDocId));

            if(stripeAccount == null || !stripeAccount.IsActive)
            {
                return new MerchantPaymentEligibilityDto
                {
                    IsEligible = false,
                    Reason = "Stripe account not found Or Inactive for the merchant."
                };
            }
        }

        return new MerchantPaymentEligibilityDto
        {
            IsEligible = true,
            Reason = "Merchant is eligible for payment processing."
        };
    }
}
