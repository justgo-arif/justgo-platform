using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.DTOs.PaymentConsoleDtos;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetMerchantEntityIdentifiers;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetOwnerId;
using JustGo.Finance.Application.Features.PaymentAccount.Queries.GetAdyenPaymentAccountDetails;
using JustGo.Finance.Application.Features.PaymentAccount.Queries.GetStripeMerchantProfile;

namespace JustGo.Finance.Application.Features.PaymentConsole.Queries.GetMerchantPaymentVisibility
{
    public class GetMerchantPaymentVisibilityHandler : IRequestHandler<GetMerchantPaymentVisibilityQuery, MerchantPaymentVisibilityDto>
    {
        private readonly ISystemSettingsService _systemSettingsService;
        private readonly IMediator _mediator;
        private readonly LazyService<IReadRepository<string>> _readRepository;


        public GetMerchantPaymentVisibilityHandler(
            ISystemSettingsService systemSettingsService,
            IMediator mediator,
            LazyService<IReadRepository<string>> readRepository)
        {
            _systemSettingsService = systemSettingsService;
            _mediator = mediator;
            _readRepository = readRepository;
        }
        public async Task<MerchantPaymentVisibilityDto> Handle(
            GetMerchantPaymentVisibilityQuery request,
            CancellationToken cancellationToken)
        {
            var response = new MerchantPaymentVisibilityDto
            {
                MerchantId = request.MerchantId,
                IsMerchantPaymentEligible = true,
                IsPaymentEligible = false,
                Reason = "Merchant is eligible for payment processing."
            };

            var ownerId = await _mediator.Send(
                new GetOwnerIdQuery(request.MerchantId),
                cancellationToken);

            MerchantEntityIdentifiersDto? merchantEntityIdentifiers = null;

            try
            {
                merchantEntityIdentifiers = await _mediator.Send(
                    new GetMerchantEntityIdentifiersQuery(request.MerchantId),
                    cancellationToken);
            }
            catch
            {
                return new MerchantPaymentVisibilityDto
                {
                    MerchantId = request.MerchantId,
                    IsMerchantPaymentEligible = false,
                    IsPaymentEligible = false,
                    Reason = "Merchant not found."
                };
            }

            if (merchantEntityIdentifiers is null)
            {
                return new MerchantPaymentVisibilityDto
                {
                    MerchantId = request.MerchantId,
                    IsMerchantPaymentEligible = false,
                    IsPaymentEligible = false,
                    Reason = "Merchant not found."
                };
            }

            if (ownerId == 0)
            {
                response.IsPaymentEligible = true;
            }
            else
            {
                var query = @"
            SELECT CASE 
                WHEN EXISTS (
                    SELECT 1 
                    FROM SystemSettings 
                    WHERE ItemKey = 'ORGANISATION.TYPE' 
                      AND [Value] = 'STAND-ALONE-CLUB-PLUS'
                ) THEN CAST(1 AS BIT)

                WHEN EXISTS (
                    SELECT 1
                    FROM Hierarchies H
                    INNER JOIN GoMembershipRegistry GMR 
                        ON GMR.EntityId = H.EntityId 
                       AND GMR.[Status] = 1
                    WHERE H.EntityId = @merchantDocId
                ) THEN CAST(1 AS BIT)

                ELSE CAST(0 AS BIT)
            END";

                var parameters = new DynamicParameters();
                parameters.Add("merchantDocId", ownerId);

                var data = await _readRepository.Value
                    .GetSingleAsync(query, cancellationToken, parameters, null, "text");

                response.IsPaymentEligible = data != null && Convert.ToBoolean(data);
            }

            var enableAdyenPayment = await _systemSettingsService
                .GetSystemSettingsByItemKey("SYSTEM.PAYMENT.EnableAdyenPayment", cancellationToken);

            bool isAdyenEnabled = enableAdyenPayment != null
                && bool.TryParse(enableAdyenPayment, out var isEnabled)
                && isEnabled;

            if (isAdyenEnabled)
            {
                var adyenAccount = await _mediator.Send(
                    new GetAdyenPaymentAccountDetailsQuery(merchantEntityIdentifiers.MerchantGuid),
                    cancellationToken);

                if (adyenAccount == null ||
                    adyenAccount.Problems?.Count > 0 ||
                    !adyenAccount.IsPaymentEnabled)
                {
                    response.IsMerchantPaymentEligible = false;
                    response.Reason = "Adyen account not found or not properly configured.";
                }
            }
            else
            {
                var stripeAccount = await _mediator.Send(
                    new GetStripeMerchantProfileQuery((int)merchantEntityIdentifiers.MerchantId),
                    cancellationToken);

                if (stripeAccount == null || !stripeAccount.IsActive)
                {
                    response.IsMerchantPaymentEligible = false;
                    response.Reason = "Stripe account not found or inactive.";
                }
            }

            return response;
        }

    }

}
