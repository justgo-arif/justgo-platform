using System.Threading;
using Adyen.Model.BalancePlatform;
using Adyen.Model.ConfigurationWebhooks;
using Adyen.Service.BalancePlatform;
using Azure.Core;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.Interfaces;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
namespace JustGo.Finance.Application.Features.PaymentAccount.Commands.UpdateSweep;

public class UpdateSweepHandler : IRequestHandler<UpdateSweepCommand, bool>
{
    private readonly IAdyenClientFactory _adyenClientFactory;
    private readonly LazyService<IWriteRepository<string>> _writeRepository;
    private readonly ICustomError _error;

    public UpdateSweepHandler(IAdyenClientFactory adyenClientFactory, LazyService<IWriteRepository<string>> writeRepository, ICustomError error)
    {
        _adyenClientFactory = adyenClientFactory;
        _writeRepository = writeRepository;
        _error = error;
    }

    public async Task<bool> Handle(UpdateSweepCommand request, CancellationToken cancellationToken)
    {
        string cronExpression = GetCronExpression(request.SweepType, request.DayOfWeek, request.DayOfMonth);

        try
        {
            var sweepConfiguration = new UpdateSweepConfigurationV2
            {
                Schedule = new Adyen.Model.BalancePlatform.SweepSchedule
                {
                    Type = Adyen.Model.BalancePlatform.SweepSchedule.TypeEnum.Cron,
                    CronExpression = cronExpression
                }
            };
            var client = await _adyenClientFactory.CreateClientAsync(DTOs.Enums.AdyenKeyType.BalancePlatform, cancellationToken);
            if (client == null) { 
                _error.NotFound<object>("Adyen client not found.");
                return false;
            }

            try {
                var service = new BalanceAccountsService(client);
                var response = await service.UpdateSweepAsync(request.BalanceAccountId, request.SweepId, sweepConfiguration);
            }
            catch (Adyen.HttpClient.HttpClientException ex)
            {
                _error.CustomValidation<object>($"Error creating sweep configuration: {ex.Message}");
                return false;
            }

            await UpdatePayoutSchedule(request.BalanceAccountId, request.SweepType.ToString(), request.SweepId, cancellationToken);
        }
        catch (Adyen.HttpClient.HttpClientException)
        {
            return false;
        }
        return true;
    }

    private string GetCronExpression(
            Adyen.Model.BalancePlatform.SweepSchedule.TypeEnum typeEnum,
            int? dayOfWeek = null,  // 1 (Monday) to 7 (Sunday)
            int? dayOfMonth = null  // 1 to 31
        )
    {
        string cronExpression = string.Empty;

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
        }

        return cronExpression;
    }

    private async Task UpdatePayoutSchedule(string balanceAccountId, string payoutSchedule, string sweepId, CancellationToken cancellationToken) {
        string query = @"
             Update [AdyenBalanceAccounts] 
                 SET 
                    PayoutSchedule = @payoutSchedule,
                    [SweepId] = @SweepId,
                    UpdatedDate = GETUTCDATE()
             WHERE [BalanceAccountId] = @BalanceAccountId
            ";

        var queryParameters = new DynamicParameters();
        queryParameters.Add("@payoutSchedule", payoutSchedule);
        queryParameters.Add("@SweepId", sweepId);
        queryParameters.Add("@BalanceAccountId", balanceAccountId);

        await _writeRepository.Value.ExecuteAsync(query, cancellationToken, queryParameters, null, "text");
    }
}
