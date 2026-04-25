using System.Data;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.Common.Constants;
using JustGo.Finance.Application.Common.Helpers;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.Features.GetDocIdBySyncGuid;
using JustGo.Finance.Application.Features.PaymentRefund.Queries.GetRefundProduct;
using JustGo.Finance.Domain.Entities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Finance.Application.Features.PaymentRefund.Commands.CreateRefundPayment
{

    public class CreateRefundPaymentHandler : IRequestHandler<CreateRefundPaymentCommand, string>
    {
        private readonly IMediator _mediator;
        private readonly LazyService<IReadRepository<string>> _readRepository;
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;

        public CreateRefundPaymentHandler(IMediator mediator, LazyService<IReadRepository<string>> readRepository, IWriteRepositoryFactory writeRepository, IUnitOfWork unitOfWork, IUtilityService utilityService)
        {
            _mediator = mediator;
            _readRepository = readRepository;
            _writeRepository = writeRepository;
            _unitOfWork = unitOfWork;
            _utilityService = utilityService;
        }

        public async Task<string> Handle(CreateRefundPaymentCommand command, CancellationToken cancellationToken)
        {
            var currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            using var dbTransaction = await _unitOfWork.BeginTransactionAsync();

            var result = await SaveRefundRequest(command, currentUserId, cancellationToken, dbTransaction);

            if (result <= 0)
            {
                await _unitOfWork.RollbackAsync(dbTransaction);
                return "Failed to create refund request.";
            }

            await InsertRefundItemsAsync(command, result, currentUserId, cancellationToken, dbTransaction);

            await _unitOfWork.CommitAsync(dbTransaction);
            return command.PaymentId.ToString();

        }
        public async Task<int> SaveRefundRequest(CreateRefundPaymentCommand command, int currentUserId, CancellationToken cancellationToken, IDbTransaction dbTransaction)
        {
            var paymentDocId = await _mediator.Send(new GetDocIdBySyncGuidQuery(command.PaymentId), cancellationToken);
            var sqlParameters = SqlParameterHelper.CreateParams(
                                    ("PaymentId", paymentDocId),
                                    ("RefundType", command.RequestRefundType.ToString()),
                                    ("RefundReasonId", command.RefundReasonId),
                                    ("RefundNote", command.RefundNote),
                                    ("IsSendNotification", command.IsSendNotification),
                                    ("CreatedBy", currentUserId),
                                    ("CreatedDate", DateTime.Now)
                                );
            var sql = @"
                                INSERT INTO RefundRequestInfo (
                                    PaymentId,
                                    RefundType,
                                    RefundReasonId,
                                    RefundNote,
                                    IsSendNotification,
                                    CreatedBy,
                                    CreatedDate
                                )
                                OUTPUT INSERTED.Id
                                VALUES (
                                    @PaymentId,
                                    @RefundType,
                                    @RefundReasonId,
                                    @RefundNote,
                                    @IsSendNotification,
                                    @CreatedBy,
                                    @CreatedDate
                                );";
            var result = await _writeRepository.GetLazyRepository<RefundRequest>().Value.ExecuteAsync(sql, cancellationToken, sqlParameters, dbTransaction, "text");
            CustomLog.Event("User Changed|Payment|Create", currentUserId, paymentDocId, EntityType.Finance, 0, "New Refund Create");
            return result;
        }

        public async Task InsertRefundItemsAsync(CreateRefundPaymentCommand command, int RefundRequestId, int actionUserId, CancellationToken cancellationToken, IDbTransaction dbTransaction)
        {
            var paymentDocId = await _mediator.Send(new GetDocIdBySyncGuidQuery(command.PaymentId), cancellationToken);
            var paymentId = await _readRepository.Value
                             .GetSingleAsync(SqlQueries.SelectPaymentIdByDocId, cancellationToken, QueryHelpers.GetPaymentDocIdParams(paymentDocId), null, "text");

            var insertRefundRequestInfoDetails = @"INSERT INTO RefundRequestInfoDetails (
                                            RefundRequestId,
                                            RowId,
                                            PaymentDocId,
                                            ProductDocId,
                                            ForEntityDocId,
                                            RequestRefundType,
                                            RequestRefundAmmount,
                                            ApplicableRefundAmmount,
                                            RequestRefundWithPaymentFees,
                                            ActionUserId,
                                            CreationDate
                                        ) VALUES (
                                            @RefundRequestId,
                                            @RowId,
                                            @PaymentDocId,
                                            @ProductDocId,
                                            @ForEntityDocId,
                                            @RequestRefundType,
                                            @RequestRefundAmmount,
                                            @ApplicableRefundAmmount,
                                            @RequestRefundWithPaymentFees,
                                            @ActionUserId,
                                            @CreationDate
                                        );";
            var sql = @"INSERT INTO PaymentRefundQueue (
                            RefundArea,
                            PaymentDocId,
                            PaymentId,
                            ProductDocId,
                            TypeEntityDocId,
                            ForEntityDocId,
                            CreationDate,
                            Executed,
                            ExecutionDate,
                            ExecutionDetails,
                            RequestRefundType,
                            RequestRefundAmmount,
                            ApplicableRefundAmmount,
                            RequestRefundWithPaymentFees,
                            ActionUserId,
                            RowId
                        )
                        VALUES (
                            @RefundArea,
                            @PaymentDocId,
                            @PaymentId,
                            @ProductDocId,
                            @TypeEntityDocId,
                            @ForEntityDocId,
                            @CreationDate,
                            @Executed,
                            @ExecutionDate,
                            @ExecutionDetails,
                            @RequestRefundType,
                            @RequestRefundAmmount,
                            @ApplicableRefundAmmount,
                            @RequestRefundWithPaymentFees,
                            @ActionUserId,
                            @RowId
                        );"
            ;

            if (command.RequestRefundType == RefundType.Full)
            {
                var query = new GetRefundableItemsQuery(command.PaymentId)
                {
                    MerchantId = command.MerchantId,
                    MemberId = command.MemberId,
                    Source = command.Source
                };
                var result = await _mediator.Send(
                    query,
                    cancellationToken
                );

                command.RefundItems = result
                    .Where(item => Guid.TryParse(item.ProductId, out _))
                    .Select(item => new RefundableItemCommand
                    {
                        RowId = item.RowId,
                        Code = item.Code,
                        ItemName = item.ItemName,
                        ItemDescription = item.ItemDescription,
                        ProductImageURL = item.ProductImageURL,
                        Comment = item.Comment,
                        OriginalAmount = item.OriginalAmount,
                        AmountRemaining = item.AmountRemaining,
                        AmountToRefund = item.AmountToRefund,
                        MemberName = item.MemberName,
                        MemberId = item.MemberId,
                        ForEntityDocId = item.ForEntityDocId,
                        ProfilePicURL = item.ProfilePicURL,
                        ProductId = Guid.Parse(item.ProductId!)
                    })
                    .ToList();
            }


            foreach (var item in command.RefundItems ?? Enumerable.Empty<RefundableItemCommand>())
            {
                var productDocId = await _mediator.Send(new GetDocIdBySyncGuidQuery(item.ProductId), cancellationToken);
                var parameters = new
                {
                    RefundArea = "Payment",
                    PaymentDocId = paymentDocId,
                    PaymentId = paymentId,
                    ProductDocId = productDocId,
                    TypeEntityDocId = 0,
                    ForEntityDocId = item.ForEntityDocId,
                    CreationDate = DateTime.UtcNow,
                    Executed = false,
                    ExecutionDate = (DateTime?)null,
                    ExecutionDetails = (string?)null,
                    RequestRefundType = (int)command.RequestRefundType,
                    RequestRefundAmmount = item.AmountToRefund,
                    ApplicableRefundAmmount = item.AmountRemaining,
                    RequestRefundWithPaymentFees = 0,
                    ActionUserId = actionUserId,
                    RowId = item.RowId
                };
                await _writeRepository.GetLazyRepository<RefundRequest>().Value.ExecuteAsync(sql, cancellationToken, parameters, dbTransaction, "text");
                var refundRequestInfoParams = new
                {
                    RefundRequestId = RefundRequestId,
                    RowId = item.RowId,
                    PaymentDocId = paymentDocId,
                    ProductDocId = productDocId,
                    ForEntityDocId = item.ForEntityDocId,
                    RequestRefundType = (int)command.RequestRefundType,
                    RequestRefundAmmount = item.AmountToRefund,
                    ApplicableRefundAmmount = item.AmountRemaining,
                    RequestRefundWithPaymentFees = 0,
                    ActionUserId = actionUserId,
                    CreationDate = DateTime.UtcNow
                };
                await _writeRepository.GetLazyRepository<RefundRequest>().Value.ExecuteAsync(insertRefundRequestInfoDetails, cancellationToken, refundRequestInfoParams, dbTransaction, "text");
            }
        }
    }

}
