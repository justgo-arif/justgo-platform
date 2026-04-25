using System.Data;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Services.Interfaces.CustomErrors;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.Features.GetDocIdBySyncGuid;
using JustGo.Finance.Application.Features.GetUserIdBySyncGuid;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetOwnerId;
using JustGo.Finance.Application.Features.PaymentConsole.Commands.AddProductToCart;
using JustGo.Finance.Application.Features.PaymentConsole.Queries.GetPaymentConsoleProduct;

namespace JustGo.Finance.Application.Features.PaymentConsole.Commands.CreatePaymentConsole
{
    public class CreatePaymentConsoleCommandHandler : IRequestHandler<CreatePaymentConsoleCommand, string>
    {
        private readonly LazyService<IReadRepository<string>> _readRepository;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;
        private readonly IMediator _mediator;
        private readonly ICustomError _error;

        public CreatePaymentConsoleCommandHandler(LazyService<IReadRepository<string>> readRepository, IWriteRepositoryFactory writeRepository, IUnitOfWork unitOfWork, IUtilityService utilityService, IMediator mediator, ICustomError error)
        {
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _unitOfWork = unitOfWork;
            _utilityService = utilityService;
            _mediator = mediator;
            _error = error;
        }

        public async Task<string> Handle(CreatePaymentConsoleCommand command, CancellationToken cancellationToken)
        {
            var currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            var ownerId = await _mediator.Send(new GetOwnerIdQuery(command.PayTo), cancellationToken);

            int result = -1;
            if (command.PaymentMethod == PaymentConsolePaymentMethods.AutoCharge)
            {
                result = await SaveAutoPaymentRequest(command, currentUserId, ownerId, dbTransaction, cancellationToken);
            }
            else if (command.PaymentMethod == PaymentConsolePaymentMethods.CartCheckout)
            {
                var isSuccess = await _mediator.Send(new AddConsoleProductToCartCommand(command.BillingType, ownerId, command.Customers, command.Products), cancellationToken);

                result = isSuccess ? 1 : 0;
            }

            if (result <= 0)
            {
                await _unitOfWork.RollbackAsync(dbTransaction);
                _error.CustomValidation<object>("Failed to create payment console request.");
                return "Error";
            }

            await _unitOfWork.CommitAsync(dbTransaction);
            return "Success";

        }
        private async Task<int> SaveAutoPaymentRequest(CreatePaymentConsoleCommand command, int currentUserId, int productOwnerId, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {
            var ownerProducts = await _mediator.Send(
                       new GetPaymentConsoleProductQuery(productOwnerId), cancellationToken);

            foreach (var customer in command.Customers)
            {
                int recurringPaymentCustomerId = 0;
                var userId = await _mediator.Send(new GetUserIdBySyncGuidQuery(Guid.Parse(customer.EntityId)), cancellationToken);
                var forEntityId = await _mediator.Send(new GetDocIdBySyncGuidQuery(Guid.Parse(customer.EntityId)), cancellationToken);
                if (customer.RecurringPaymentCustomerId != null && customer.RecurringPaymentCustomerId > 0)
                {
                    var existingRecurringPaymentCustomer = await GetExistingRecurringPaymentCustomer((int)customer.RecurringPaymentCustomerId, cancellationToken);
                    if (existingRecurringPaymentCustomer == null)
                    {
                        _error.CustomValidation<object>($"Existing Recurring Payment Customer Not Found. For id: {customer.RecurringPaymentCustomerId}");
                        CustomLog.Event("User Changed|Payment|Create", currentUserId, 0, EntityType.Finance, 0, $"Existing Recurring Payment Customer Not Found. For id: {customer.RecurringPaymentCustomerId}");
                        return -1;
                    }
                    recurringPaymentCustomerId = (int)customer.RecurringPaymentCustomerId;
                }
                else
                {
                    recurringPaymentCustomerId = await CreateRecurringPaymentCustomerAsync(userId, currentUserId, dbTransaction, cancellationToken);  //Create if recurring payment customer not found
                }

                foreach (var item in command.Products)
                {
                    var matchedProduct = ownerProducts.FirstOrDefault(x => x.CategoryId == item.CategoryId);
                    if (matchedProduct == null)
                    {
                        CustomLog.Event("User Changed|Payment|Create", currentUserId, 0, EntityType.Finance, 0, $"Error in creating console payment. Product Not Found for merchant : {command.PayTo}");
                        return -1;
                    }
                    var consoleparameters = new
                    {
                        CustomerId = recurringPaymentCustomerId,
                        ProductId = matchedProduct.ProductId,
                        EntityId = forEntityId,
                        ScheduleDate = command.ChargeDate?.ToString("yyyy-MM-dd"),
                        ActionUserId = currentUserId,
                        item.Amount,
                        item.Description
                    };
                    var recurringconsolesql = "[dbo].[CreateRecurringPaymentPlanWithPrice]";
                    await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(recurringconsolesql, cancellationToken, consoleparameters, dbTransaction);
                }

                var sendemail = "[dbo].[SEND_EMAIL_BY_SCHEME]";

                var messageScheme = (customer.RecurringPaymentCustomerId == null || customer.RecurringPaymentCustomerId == 0)
                    ? "Payment/Payment Console"
                    : "Payment/Payment Console Active Card";

                var emailparameters = new
                {
                    MessageScheme = messageScheme,
                    Argument = "",
                    ForEntityId = forEntityId,
                    TypeEntityId = recurringPaymentCustomerId,
                    InvokeUserId = currentUserId,
                    OwnerType = productOwnerId > 0 ? "Club" : "NGB",
                    OwnerId = productOwnerId,
                    TestEmailAddress = "N/A",
                    GetInfo = 0,
                    MessageDocId = -1
                };

                await _writeRepository.GetLazyRepository<object>().Value.ExecuteAsync(
                    sendemail,
                    cancellationToken,
                    emailparameters,
                    dbTransaction
                );


            }

            CustomLog.Event("User Changed|Payment|Create", currentUserId, 0, EntityType.Finance, 0, "New Payment Console Create");
            return 1;
        }

        private async Task<int> CreateRecurringPaymentCustomerAsync(int userId, int currentUserId, IDbTransaction dbTransaction, CancellationToken cancellationToken)
        {


            var sql = @"
                IF EXISTS(select 1 from RecurringPaymentCustomer Where OwnerUserId = @OwnerUserId AND  PaymentMethod = 'PendingCardCapture')
                 BEGIN
	                select Top 1 Id  from RecurringPaymentCustomer Where OwnerUserId = @OwnerUserId AND  PaymentMethod = 'PendingCardCapture'
                 END
                 ELSE
                 BEGIN
	                INSERT INTO RecurringPaymentCustomer (
                                    OwnerUserId,
                                    PaymentArguments,
                                    PaymentMethod,
                                    ProviderSignature,
                                    MetaData,
                                    Tag,
                                    Status,
                                    Created,
                                    CreatedUser
                                ) 
                                OUTPUT INSERTED.Id
                                VALUES (
                                    @OwnerUserId,
                                    @PaymentArguments,
                                    @PaymentMethod,
                                    @ProviderSignature,
                                    @MetaData,
                                    @Tag,
                                    @Status,
                                    @Created,
                                    @CreatedUser
                                ) 
                 END";

            var parameters = new
            {
                OwnerUserId = userId,
                PaymentArguments = "",
                PaymentMethod = "PendingCardCapture",
                ProviderSignature = "",
                MetaData = "",
                Tag = "",
                Status = 2,
                Created = DateTime.UtcNow,
                CreatedUser = currentUserId
            };

            var result = await _writeRepository
                .GetLazyRepository<object>().Value
                .ExecuteScalarAsync<int>(sql, cancellationToken, parameters, dbTransaction, "text");

            return result;
        }
        private async Task<int?> GetExistingRecurringPaymentCustomer(int recurringPaymentCustomerId, CancellationToken cancellationToken)
        {
            var query = @"SELECT Id FROM RecurringPaymentCustomer WHERE Id = @recurringPaymentCustomerId";

            var parameters = new DynamicParameters();
            parameters.Add("recurringPaymentCustomerId", recurringPaymentCustomerId);

            var data = await _readRepository.Value.GetSingleAsync(query, cancellationToken, parameters, null, "text");

            if (data == null) return null;

            return (int)data;
        }
    }
}
