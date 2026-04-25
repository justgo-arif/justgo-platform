using Adyen.Model.BalancePlatform;
using Adyen.Model.LegalEntityManagement;
using Adyen.Service.BalancePlatform;
using Adyen.Service.LegalEntityManagement;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs.PaymentAccount;
using JustGo.Finance.Application.Features.Balances.Queries.GetAdyenBalanceAccounts;
using JustGo.Finance.Application.Features.PaymentAccount.Queries.GetSavedAdyenAccountDetails;
using JustGo.Finance.Application.Interfaces;
using JustGo.Finance.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentAccount.Queries.GetAdyenPaymentAccountDetails;

public class GetAdyenPaymentAccountDetailsHandler : IRequestHandler<GetAdyenPaymentAccountDetailsQuery, AdyenPaymentProfileDetailsDTO?>
{
    private readonly LazyService<IReadRepository<AdyenPaymentProfileDetailsDTO>> _readRepository;
    private readonly IMediator _mediator;
    private readonly IAdyenClientFactory _adyenClientFactory;
    private readonly ISystemSettingsService _systemSettingsService;
    private readonly ICustomError _error;


    public GetAdyenPaymentAccountDetailsHandler(
        LazyService<IReadRepository<AdyenPaymentProfileDetailsDTO>> readRepository,
        IMediator mediator,
        IAdyenClientFactory adyenClientFactory,
        ISystemSettingsService systemSettingsService,
        ICustomError error)
    {
        _readRepository = readRepository;
        _mediator = mediator;
        _adyenClientFactory = adyenClientFactory;
        _systemSettingsService = systemSettingsService;
        _error = error;
    }

    public async Task<AdyenPaymentProfileDetailsDTO?> Handle(GetAdyenPaymentAccountDetailsQuery request, CancellationToken cancellationToken)
    {

        var localAccount = await _mediator.Send(new GetSavedAdyenAccountDetailsQuery(request.MerchantId));

        if (localAccount is null || localAccount.LegalEntityId is null)
        {
            _error.NotFound<object>("Payment profile details not found for the given merchant.");
            return null;
        }

        var localBalanceAccounts = await _mediator.Send(new GetAdyenBalanceAccountsQuery(request.MerchantId));
        if (localBalanceAccounts is null || !localBalanceAccounts.Any())
        {
            _error.NotFound<object>("No balance accounts found for the given merchant.");
            return null;
        }
        var balanceAccount = localBalanceAccounts.First();

        var legalEntityDetails = await GetLegalEntityDetailsFromAdyenAsync(localAccount.LegalEntityId, cancellationToken);

        if (legalEntityDetails is null)
        {
            _error.NotFound<object>("Legal entity details not found for the given legal entity ID.");
            return null;
        }

        var problems = new List<string>();
        if (legalEntityDetails.Problems is not null)
        {
            foreach (var problem in legalEntityDetails.Problems)
            {
                if (problem.VerificationErrors is not null)
                {
                    problems.AddRange(problem.VerificationErrors.Select(error => error.Message));
                }
            }
        }

        legalEntityDetails.Capabilities.TryGetValue("receiveFromBalanceAccount", out var receiveFromBalanceAccount);
        legalEntityDetails.Capabilities.TryGetValue("sendToBalanceAccount", out var sendToBalanceAccount);
        legalEntityDetails.Capabilities.TryGetValue("sendToTransferInstrument", out var sendToTransferInstrument);
        legalEntityDetails.Capabilities.TryGetValue("receiveFromPlatformPayments", out var receiveFromPlatformPayments);

        var profileDetails = new AdyenPaymentProfileDetailsDTO
        {
            LegalEntityId = localAccount.LegalEntityId,
            BalanceAccountId = balanceAccount.BalanceAccountId,
            Type = legalEntityDetails.Type.ToString(),
            DoingBusinessAs = legalEntityDetails.Organization != null ? legalEntityDetails.Organization.DoingBusinessAs : null,
            Email = legalEntityDetails.Organization != null ? legalEntityDetails.Organization.Email : null,
            Country = legalEntityDetails.Organization != null ? legalEntityDetails.Organization.RegisteredAddress.Country : null,
            ReceiveFromBalanceAccount = receiveFromBalanceAccount?.Allowed ?? false,
            SendToBalanceAccount = sendToBalanceAccount?.Allowed ?? false,
            SendToTransferInstrument = sendToTransferInstrument?.Allowed ?? false,
            ReceiveFromPlatformPayments = receiveFromPlatformPayments?.Allowed ?? false,
            Problems = problems
        };

        if (!String.IsNullOrEmpty(localAccount.BusinessLineId))
        {
            var businessLineInfo = await GetBusinessLineFromAdyenAsync(localAccount.BusinessLineId, cancellationToken);
            if (businessLineInfo != null && businessLineInfo.WebData != null && businessLineInfo.WebData.Count > 0)
            {
                profileDetails.WebData = businessLineInfo.WebData[0].WebAddress;
            }
        }

        if (!string.IsNullOrEmpty(balanceAccount.SweepId)) {
            var sweepDetails = await GetSweepFromAdyenAsync(balanceAccount.BalanceAccountId, balanceAccount.SweepId, cancellationToken);
            if(sweepDetails != null)
            {
                profileDetails.SweepId = sweepDetails.Id;
                profileDetails.SweepDescription = sweepDetails.Description;
                profileDetails.DefaultCurrency = sweepDetails.Currency;
                var cron = sweepDetails.Schedule.CronExpression;
                if (!String.IsNullOrEmpty(cron))
                {
                    var cronDetails = ParseCron(cron);

                    profileDetails.PayoutSchedule = cronDetails.PayoutSchedule;
                    profileDetails.DayOfMonth = cronDetails.DayOfMonth;
                    profileDetails.DayOfWeek = cronDetails.DayOfWeek;
                }
                else
                {
                    profileDetails.PayoutSchedule = sweepDetails.Schedule.Type.ToString();
                }
            }
        }
        else
        {
            if (legalEntityDetails.TransferInstruments != null)
            {
                await CreateSweepAsync(
                    balanceAccount.BalanceAccountId,
                    legalEntityDetails.TransferInstruments[0].Id,
                    cancellationToken
                );
            }
        }

        profileDetails.IsPaymentEnabled = false;
        var accountHolderId = balanceAccount.AccountHolderId;

        var accountHolder = await GetAccountHolderFromAdyenAsync(accountHolderId!, cancellationToken);
        if (accountHolder != null && accountHolder.Capabilities != null)
        {
            if (accountHolder.Capabilities.TryGetValue("receivePayments", out var capability))
            {
                if (capability.Enabled == true && capability.Allowed == true)
                {
                    profileDetails.IsPaymentEnabled = true;
                }
            }
        }
        profileDetails.StatementDescriptor = localAccount.StatementDescriptor;

        return profileDetails;
    }

    private async Task<LegalEntity?> GetLegalEntityDetailsFromAdyenAsync(string legalEntityId, CancellationToken cancellationToken)
    {
        try
        {
            var client = await _adyenClientFactory.CreateClientAsync(DTOs.Enums.AdyenKeyType.LegalEntity, cancellationToken);
            if (client == null) return null;
            var service = new LegalEntitiesService(client);
            var response = await service.GetLegalEntityAsync(legalEntityId);
            return response;
        }
        catch (Adyen.HttpClient.HttpClientException)
        {
            return null;
        }
    }

    private async Task<BusinessLine?> GetBusinessLineFromAdyenAsync(string businessLineId, CancellationToken cancellationToken)
    {
        try
        {
            var client = await _adyenClientFactory.CreateClientAsync(DTOs.Enums.AdyenKeyType.LegalEntity, cancellationToken);
            if (client == null) return null;
            var service = new BusinessLinesService(client);
            var response = await service.GetBusinessLineAsync(businessLineId);
            return response;
        }
        catch (Adyen.HttpClient.HttpClientException)
        {
            return null;
        }
    }

    private async Task<SweepConfigurationV2?> GetSweepFromAdyenAsync(string balanceAccountId, string sweepId, CancellationToken cancellationToken)
    {
        try
        {
            var client = await _adyenClientFactory.CreateClientAsync(DTOs.Enums.AdyenKeyType.BalancePlatform, cancellationToken);
            if (client == null) return null;
            var service = new BalanceAccountsService(client);
            var response = await service.GetSweepAsync(balanceAccountId, sweepId, cancellationToken: cancellationToken);
            return response;
        }
        catch (Adyen.HttpClient.HttpClientException)
        {
            return null;
        }
    }

    private async Task<AccountHolder?> GetAccountHolderFromAdyenAsync(string accountHolderId, CancellationToken cancellationToken)
    {
        try
        {
            var client = await _adyenClientFactory.CreateClientAsync(DTOs.Enums.AdyenKeyType.BalancePlatform, cancellationToken);
            if (client == null) return null;
            var service = new AccountHoldersService(client);
            var response = await service.GetAccountHolderAsync(accountHolderId, cancellationToken: cancellationToken);
            return response;
        }
        catch (Adyen.HttpClient.HttpClientException)
        {
            return null;
        }
    }

    private async Task<SweepConfigurationV2?> CreateSweepAsync(string balanceAccountId, string destinationAccountId, CancellationToken cancellationToken)
    {
        try
        {
            var currency = await _systemSettingsService.GetSystemSettingsByItemKey("SYSTEM.CURRENCY.DEFAULTCURRENCY", cancellationToken);
            Adyen.Model.BalancePlatform.SweepSchedule.TypeEnum typeEnum = Adyen.Model.BalancePlatform.SweepSchedule.TypeEnum.Monthly;
            string cronExpression = GetCronExpression(typeEnum);
            var sweepConfiguration = new CreateSweepConfigurationV2
            {
                Schedule = new Adyen.Model.BalancePlatform.SweepSchedule
                {
                    Type = Adyen.Model.BalancePlatform.SweepSchedule.TypeEnum.Cron,
                    CronExpression = cronExpression
                },
                Currency = currency,
                Counterparty = new Adyen.Model.BalancePlatform.SweepCounterparty { TransferInstrumentId = destinationAccountId }
            };


            var client = await _adyenClientFactory.CreateClientAsync(DTOs.Enums.AdyenKeyType.BalancePlatform, cancellationToken);
            if (client == null) return null;
            var service = new BalanceAccountsService(client);
            var response = await service.CreateSweepAsync(balanceAccountId, cancellationToken: cancellationToken);
            return response;
        }
        catch (Adyen.HttpClient.HttpClientException)
        {
            return null;
        }
    }

    private string GetCronExpression(
            Adyen.Model.BalancePlatform.SweepSchedule.TypeEnum typeEnum,
            int? dayOfWeek = null,  // 1 (Monday) to 7 (Sunday)
            int? dayOfMonth = null  // 1 to 31
        )
    {
        string cronExpression;

        switch (typeEnum)
        {
            case Adyen.Model.BalancePlatform.SweepSchedule.TypeEnum.Daily:
                cronExpression = "0 0 * * *";  // Every day at midnight
                break;

            case Adyen.Model.BalancePlatform.SweepSchedule.TypeEnum.Weekly:
                int weekDay = (dayOfWeek ?? 1) % 7;  // Convert 7 (Sunday) -> 0
                cronExpression = $"0 0 * * {weekDay}";
                break;

            case Adyen.Model.BalancePlatform.SweepSchedule.TypeEnum.Monthly:
                int monthDay = dayOfMonth ?? 1; // Default to 1st
                cronExpression = $"0 0 {monthDay} * *";
                break;

            case Adyen.Model.BalancePlatform.SweepSchedule.TypeEnum.Balance:
                cronExpression = "0 0 * * *"; // Same as daily
                break;

            case Adyen.Model.BalancePlatform.SweepSchedule.TypeEnum.Cron:
                cronExpression = "*/30 * * * *"; // Every 30 minutes
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(typeEnum), $"Unsupported schedule type: {typeEnum}");
        }

        return cronExpression;
    }

    private static CronSchedule ParseCron(string cron)
    {
        var parts = cron.Split(' ');
        if (parts.Length != 5)
            throw new ArgumentException("Invalid cron expression. Expected 5 parts.");

        var minute = parts[0];
        var hour = parts[1];
        var dayOfMonth = parts[2];
        var month = parts[3];
        var dayOfWeek = parts[4];

        var schedule = new CronSchedule();

        if (dayOfWeek != "*" && dayOfMonth == "*")
        {
            // Weekly
            schedule.PayoutSchedule = "Weekly";
            if (int.TryParse(dayOfWeek, out int dowInt))
                schedule.DayOfWeek = dowInt == 0 ? 7 : dowInt;  // Convert 0 to 7 for Sunday
        }
        else if (dayOfWeek == "*" && dayOfMonth != "*")
        {
            // Monthly
            schedule.PayoutSchedule = "Monthly";
            if (int.TryParse(dayOfMonth, out int domInt))
                schedule.DayOfMonth = domInt;
        }
        else if (dayOfWeek == "*" && dayOfMonth == "*")
        {
            // Daily
            schedule.PayoutSchedule = "Daily";
        }
        else
        {
            // Ambiguous or unsupported
            schedule.PayoutSchedule = "Custom";
        }

        return schedule;
    }
}
