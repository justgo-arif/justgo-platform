using Dapper;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Newtonsoft.Json;
using System.Data;

namespace JustGo.Booking.Application.Features.BookingClasses.Commands.DeletePricingChartDiscount
{
    public class DeletePricingChartDiscountHandler : IRequestHandler<DeletePricingChartDiscountCommand, int>
    {
        private readonly IWriteRepositoryFactory _writeRepositoryFactory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;

        public DeletePricingChartDiscountHandler(IWriteRepositoryFactory writeRepositoryFactory, IUnitOfWork unitOfWork, IUtilityService utilityService)
        {
            _writeRepositoryFactory = writeRepositoryFactory;
            _unitOfWork = unitOfWork;
            _utilityService = utilityService;
        }

        public async Task<int> Handle(DeletePricingChartDiscountCommand request, CancellationToken cancellationToken = default)
        {
            var repo = _writeRepositoryFactory.GetLazyRepository<object>().Value;
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            // Soft delete details
            const string softDeleteDetailsSql = @"
                UPDATE JustGoBookingClassPricingChartDiscountDetails
                SET IsDeleted = 1
                WHERE PricingChartDiscountId = @PricingChartDiscountId;
            ";

            var parameters = new DynamicParameters();
            parameters.Add("@PricingChartDiscountId", request.PricingChartDiscountId, DbType.Int32);

            await repo.ExecuteAsync(softDeleteDetailsSql, cancellationToken, parameters, transaction, "text");

            // Soft delete discount
            const string softDeleteDiscountSql = @"
                UPDATE JustGoBookingClassPricingChartDiscount
                SET IsDeleted = 1
                WHERE PricingChartDiscountId = @PricingChartDiscountId;
            ";

            var affectedRows = await repo.ExecuteAsync(softDeleteDiscountSql, cancellationToken, parameters, transaction, "text");

            List<dynamic> audits = new List<dynamic>();
            CustomLog.Event(
                        "Class Management|PricingChartDiscount|Deleted",
                        currentUserId,
                        request.PricingChartDiscountId,
                        EntityType.ClassManagement,
                        -1,
                        "PricingChartDiscount Soft Deleted;" + JsonConvert.SerializeObject(audits)
                    );

            await _unitOfWork.CommitAsync(transaction);

            return affectedRows;
        }
    }
}