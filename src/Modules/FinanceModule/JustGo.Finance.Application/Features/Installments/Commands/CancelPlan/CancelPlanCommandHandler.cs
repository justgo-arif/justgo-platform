using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetOwnerId;
using JustGo.Finance.Domain.Entities;

namespace JustGo.Finance.Application.Features.Installments.Commands.CancelPlan
{
    public class CancelPlanCommandHandler : IRequestHandler<CancelPlanCommand, bool>
    {

        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;
        private readonly IMediator _mediator;

        public CancelPlanCommandHandler(IWriteRepositoryFactory writeRepository, IUnitOfWork unitOfWork, IUtilityService utilityService, IMediator mediator)
        {
            _writeRepository = writeRepository;
            _unitOfWork = unitOfWork;
            _utilityService = utilityService;
            _mediator = mediator;
        }

        public async Task<bool> Handle(CancelPlanCommand request, CancellationToken cancellationToken)
        {

            var ownerId = await _mediator.Send(new GetOwnerIdQuery(request.MerchantId), cancellationToken);
            var currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
            const string sql = @"update rpp set rpp.[Status]=@Status,rpp.LastModifiedDate=Getdate(),rpp.LastModifiedBy=@userId 
                                FROM RecurringPaymentCustomer rc
                                INNER JOIN RecurringPaymentPlan rpp ON rc.Id = rpp.CustomerId 
                                INNER JOIN RecurringPaymentScheme rps ON rps.Id = rpp.SchemeId
                                INNER JOIN document_11_63  pd ON pd.DocId = rpp.ProductId
                                Where rpp.PlanGuid = @PlanId
                                AND rps.RecurringType = @RecurringType
                                AND ISNULL(pd.Field_415, 0) = @OwnerId";
            var parameters = new
            {
                Status = PlanStatus.Cancel,
                UserId = currentUserId,
                PlanId = request.PlanId,
                OwnerId = ownerId,
                RecurringType = request.ScheduleRecurringType
            };
            var rowsAffected = await _writeRepository
           .GetLazyRepository<SystemAudit>()
           .Value
           .ExecuteAsync(sql, cancellationToken, parameters, dbTransaction, "text");
            await _unitOfWork.CommitAsync(dbTransaction);
            var message = rowsAffected > 0
                        ? $"Status of Recurring Payment Plan (PlanId: {request.PlanId}) successfully updated to Cancel."
                        : $"Status update attempted for Recurring Payment Plan (PlanId: {request.PlanId}), but no matching record was found or modified.";
            CustomLog.Event("User Changed|Payment|RecurringPaymentPlan Status Change", currentUserId, 0, EntityType.Finance, ownerId, message);
            return rowsAffected > 0;

        }
    }
}
