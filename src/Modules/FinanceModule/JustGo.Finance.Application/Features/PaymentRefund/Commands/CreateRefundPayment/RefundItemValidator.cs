using FluentValidation;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;

namespace JustGo.Finance.Application.Features.PaymentRefund.Commands.CreateRefundPayment
{
    public class RefundItemValidator : AbstractValidator<RefundableItemCommand>
    {
        private readonly LazyService<IReadRepository<string>> _readRepository;

        public RefundItemValidator(LazyService<IReadRepository<string>> readRepository)
        {
            _readRepository = readRepository;
            RuleFor(x => x.ProductId)
                .Must(guid => guid != Guid.Empty)
                .WithMessage("A valid Product Id is required.");
            RuleFor(x => x.ForEntityDocId)
            .NotNull().WithMessage("ForEntityDocId is required.")
            .Must(id => id != 0).WithMessage("ForEntityDocId is required.");

            RuleFor(x => x.AmountToRefund)
                .GreaterThan(0).WithMessage("Refund amount must be greater than 0.")
                .LessThanOrEqualTo(x => x.AmountRemaining)
                .WithMessage(x => $"Refund amount cannot exceed refundable amount ({x.AmountRemaining}).");

            RuleFor(x => x)
            .MustAsync(async (dto, cancellation) =>
            {
                var amountRemaining = await GetAmountRemainingAsync(dto.RowId, cancellation);
                return dto.AmountToRefund <= amountRemaining;
            })
            .WithMessage(dto => $"Refund amount cannot exceed refundable amount for item.");
        }

        private async Task<decimal> GetAmountRemainingAsync(int itemRowId, CancellationToken cancellationToken)
        {

            var sql = @";WITH RefundData AS (
                            SELECT pri.Refunditemrowid, pri.ForEntityId, pri.MerchantId, SUM(Gross) AS RefundAmount
                            FROM PaymentReceipts_Default pd
                            INNER JOIN PaymentReceipts_Items pri ON pd.DocId = pri.DocId
                            WHERE pri.Refunditemrowid = @ItemRowId
                            GROUP BY pri.Refunditemrowid, pri.ForEntityId, pri.MerchantId
                        )
                        SELECT 
                            pritems.Gross - ISNULL(rd.RefundAmount, 0) AS AmountRemaining
                        FROM PaymentReceipts_Items pritems
                        LEFT JOIN RefundData rd 
                            ON pritems.RowId = rd.Refunditemrowid 
                            AND pritems.ForEntityId = rd.ForEntityId 
                            AND pritems.MerchantId = rd.MerchantId
                        WHERE  pritems.RowId = @ItemRowId";

            var result = await _readRepository.Value.GetSingleAsync(sql, cancellationToken, new
            {
                ItemRowId = itemRowId
            }, null, "text");

            return Convert.ToDecimal(result ?? 0m);
        }
    }

}
