using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.Features.GetDocIdBySyncGuid;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetBillingAddress;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetShippingAddress;

namespace JustGo.Finance.Application.Features.MemberPayments.GetMemberPaymentSummary
{
    public class GetMemberPaymentSummaryHandler : IRequestHandler<GetMemberPaymentSummaryQuery, PaymentSummaryVM>
    {
        private readonly LazyService<IReadRepository<PaymentSummary>> _readRepository;
        private readonly IMediator _mediator;

        public GetMemberPaymentSummaryHandler(LazyService<IReadRepository<PaymentSummary>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<PaymentSummaryVM> Handle(GetMemberPaymentSummaryQuery request, CancellationToken cancellationToken)
        {

            var paymentsummaryVM = new PaymentSummaryVM();
            paymentsummaryVM.PaymentId = request.PaymentId;

            var docId = await _mediator.Send(new GetDocIdBySyncGuidQuery(request.PaymentId), cancellationToken);
            var memberdocid = await _mediator.Send(
               new GetDocIdBySyncGuidQuery(request.MemberId), cancellationToken);
            paymentsummaryVM.BillingDetails = await _mediator.Send(new GetBillingAddressQuery(request.PaymentId), cancellationToken) ?? new Address();

            paymentsummaryVM.ShippingDetails = await _mediator.Send(new GetShippingAddressQuery(request.PaymentId), cancellationToken) ?? new Address();



            var queryParameters = new DynamicParameters();
            queryParameters.Add("DocId", docId);
            queryParameters.Add("Memberdocid", memberdocid);

            var paymentSummarySQL = @$"SELECT 
                                    SUM(pritems.Gross) as TotalAmount,
                                    SUM(pritems.Quantity * pritems.Price) as SubTotal,
                                    SUM(pritems.Tax) as TaxAmount,
                                    CASE 
                                        WHEN (SUM(pritems.Gross) - SUM(pritems.Tax)) = 0 THEN 0
                                        ELSE (SUM(pritems.Tax) / (SUM(pritems.Gross) - SUM(pritems.Tax))) * 100
                                    END as TaxRate,
                                    SUM(pritems.Discount) as DiscountAmount,
                                    ISNULL(STRING_AGG(pvd.Code, ','), '') as DiscountCode,
                                    COUNT(*) as ItemsCount
                                    FROM PaymentReceipts_Default prd
                                    INNER JOIN PaymentReceipts_Items pritems ON prd.DocId=pritems.DocId
                                    INNER JOIN [User] u on prd.Paymentuserid = u.Userid
                                    left join PaymentReceipts_Voucherdiscountevaluations pvd on pvd.Purchaseitemid=pritems.RowId
                                    WHERE    prd.DocId =@DocId AND u.MemberDocId = @MemberDocId
                                    Group BY prd.PaymentId ";

            var paymentSummaryResult = await _readRepository.Value.GetAsync(paymentSummarySQL, cancellationToken, queryParameters, null, "text");
            paymentsummaryVM.PaymentSummary = paymentSummaryResult as PaymentSummary ?? new PaymentSummary();
            return paymentsummaryVM;
        }
    }
}
