using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.DTOs.InstallmentDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetOwnerId;

namespace JustGo.Finance.Application.Features.Installments.Commands.UpdatePaymentSchedule
{
    public class UpdatePaymentScheduleHandler : IRequestHandler<UpdatePaymentScheduleCommand, bool>
    {
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;
        private readonly IMediator _mediator;

        public UpdatePaymentScheduleHandler(IWriteRepositoryFactory writeRepository, IUnitOfWork unitOfWork, IUtilityService utilityService, IMediator mediator)
        {
            _writeRepository = writeRepository;
            _unitOfWork = unitOfWork;
            _utilityService = utilityService;
            _mediator = mediator;
        }

        public async Task<bool> Handle(UpdatePaymentScheduleCommand command, CancellationToken cancellationToken)
        {
            var ownerId = await _mediator.Send(new GetOwnerIdQuery(command.MerchantId), cancellationToken); 
            var currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            const string sql = @"IF @RevertToOriginalAmount = 1 
                                    BEGIN
		                                UPDATE RecurringPaymentPlan 
                                        SET Amount = (Select top  1 Unitprice from Products_Default where DocId = RecurringPaymentPlan.ProductId),
		                                PricingMode = 1
                                        WHERE Id = @PlanId

                                    END
                                    ELSE
                                    BEGIN
                                        IF  @OverrideAmount > 0
                                            BEGIN   
                                                UPDATE RecurringPaymentPlan
                                                SET Amount = @OverrideAmount,
		                                        PricingMode = 2
                                                WHERE Id = @PlanId
                                            END
                                    END
                                    update rpsch set rpsch.PaymentDate=@PaymentDate  
                                    FROM RecurringPaymentCustomer rc
                                    INNER JOIN RecurringPaymentPlan rpp ON rc.Id = rpp.CustomerId 
                                    INNER JOIN RecurringPaymentScheme rps ON rps.Id = rpp.SchemeId
                                    INNER JOIN RecurringPaymentSchedule rpsch ON rpp.Id = rpsch.PlanId
                                    INNER JOIN document_11_63  pd ON pd.DocId = rpp.ProductId
                                    Where rpsch.PlanId = @PlanId
                                    AND rpsch.Id = @Id      
                                    AND rps.RecurringType = @RecurringType
                                    AND ISNULL(pd.Field_415, 0) = @MerchantId";

            var parameters = new
            {
                PlanId = command.PlanId,
                Id = command.UpdateRequest.Id,
                PaymentDate = command.UpdateRequest.PaymentDate.Date,
                MerchantId = ownerId,
                RecurringType = RecurringType.Installment,
                RevertToOriginalAmount = command.UpdateRequest.RevertToOriginalAmount,
                OverrideAmount = command.UpdateRequest.Price.HasValue ? command.UpdateRequest.Price.Value : (decimal?)null,
            };

            var rowsAffected = await _writeRepository
                .GetLazyRepository<PaymentDateUpdateRequest>()
                .Value
                .ExecuteAsync(sql, cancellationToken, parameters, dbTransaction, "text");

            await _unitOfWork.CommitAsync(dbTransaction);


            var message = rowsAffected > 0
                        ? "Payment schedule updated successfully, including amount/date changes."
                        : "Payment update attempted, but no matching records were found or affected.";
            CustomLog.Event("User Changed|Payment|ScheduleDate Change", currentUserId, command.UpdateRequest.Id, EntityType.Finance, ownerId, message);
            return rowsAffected > 0;
        }
    }
}
