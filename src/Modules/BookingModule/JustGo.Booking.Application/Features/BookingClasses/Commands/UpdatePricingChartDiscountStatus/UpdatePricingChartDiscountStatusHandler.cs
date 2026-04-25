using Dapper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Data;

namespace JustGo.Booking.Application.Features.BookingClasses.Commands.UpdatePricingChartDiscountStatus
{
    public class UpdatePricingChartDiscountStatusHandler : IRequestHandler<UpdatePricingChartDiscountStatusCommand, int>
    {
        private readonly IWriteRepositoryFactory _writeRepositoryFactory;
        private readonly IUnitOfWork _unitOfWork;

        public UpdatePricingChartDiscountStatusHandler(
            IWriteRepositoryFactory writeRepositoryFactory,
            IUnitOfWork unitOfWork)
        {
            _writeRepositoryFactory = writeRepositoryFactory;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> Handle(UpdatePricingChartDiscountStatusCommand request, CancellationToken cancellationToken = default)
        {
            var repo = _writeRepositoryFactory.GetLazyRepository<object>().Value;
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            var sql = @"
                UPDATE JustGoBookingClassPricingChartDiscount
                SET PricingChartDiscountStatus = @PricingChartDiscountStatus
                WHERE PricingChartDiscountId = @PricingChartDiscountId AND IsDeleted = 0;
            ";

            var parameters = new DynamicParameters();
            parameters.Add("@PricingChartDiscountId", request.PricingChartDiscountId, DbType.Int32);
            parameters.Add("@PricingChartDiscountStatus", request.PricingChartDiscountStatus);

            int affectedRows = await repo.ExecuteAsync(sql, cancellationToken, parameters, transaction, "Text");

            await _unitOfWork.CommitAsync(transaction);

            return affectedRows;
        }
    }
}
