using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Finance.Application.DTOs.Enums;
using JustGo.Finance.Application.Features.GetDocIdBySyncGuid;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetMerchantId;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetBillingAddress;
using JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetShippingAddress;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentSummary
{
    public class GetPaymentSummaryHandler : IRequestHandler<GetPaymentSummaryQuery, PaymentSummaryVM>
    {
        private readonly LazyService<IReadRepository<PaymentSummary>> _readRepository;
        private readonly IMediator _mediator;

        public GetPaymentSummaryHandler(LazyService<IReadRepository<PaymentSummary>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<PaymentSummaryVM> Handle(GetPaymentSummaryQuery request, CancellationToken cancellationToken)
        {

            var paymentsummaryVM = new PaymentSummaryVM();
            paymentsummaryVM.PaymentId = request.PaymentId;

            var docId = await _mediator.Send(new GetDocIdBySyncGuidQuery(request.PaymentId), cancellationToken);

            var queryParameters = new DynamicParameters();
            queryParameters.Add("DocId", docId);

            string merchantFilter = "";
            string memberFilter = "";
            if (request.Source == RequestSource.Member)
            {
                if (!request.MemberId.HasValue)
                    throw new ArgumentException("MemberId is required for Member source.");

                var memberDocId = await _mediator.Send(new GetDocIdBySyncGuidQuery(request.MemberId.Value), cancellationToken);
                queryParameters.Add("MemberDocId", memberDocId);
                memberFilter = "AND ou.MemberDocId = @MemberDocId";
            }
            else if (request.Source == RequestSource.Merchant)
            {
                if (!request.MerchantId.HasValue)
                    throw new ArgumentException("MerchantId is required for Merchant source.");

                var merchantId = await _mediator.Send(new GetMerchantIdQuery(request.MerchantId.Value), cancellationToken);
                queryParameters.Add("MerchantId", merchantId);
                merchantFilter = "AND pritems.MerchantId = @MerchantId";
            }
            else
            {
                throw new ArgumentException("Invalid Source.");
            }

            paymentsummaryVM.BillingDetails = await _mediator.Send(new GetBillingAddressQuery(request.PaymentId), cancellationToken) ?? new Address();

            paymentsummaryVM.ShippingDetails = await _mediator.Send(new GetShippingAddressQuery(request.PaymentId), cancellationToken) ?? new Address();


             

            var paymentSummarySQL = @$"
                                     DECLARE @PAYMENTOUTPUTVERSION NVARCHAR(MAX)=(SELECT TOP 1 [Value] FROM SystemSettings WHERE ItemKey = 'ORGANISATION.PAYMENTOUTPUTVERSION')
                                     DECLARE @siteaddress NVARCHAR(MAX)=(SELECT TOP 1 [Value] FROM SystemSettings WHERE ItemKey ='SYSTEM.SITEADDRESS')

                                    SELECT 
                                    SUM(pritems.Gross) as TotalAmount,
                                    SUM(pritems.Quantity * pritems.Price) as SubTotal,
                                    SUM(pritems.Tax) as TaxAmount,
                                    CASE 
                                        WHEN (SUM(pritems.Gross) - SUM(pritems.Tax)) = 0 THEN 0
                                        ELSE (SUM(pritems.Tax) / (SUM(pritems.Gross) - SUM(pritems.Tax))) * 100
                                    END as TaxRate,
                                    SUM(pritems.Discount) as DiscountAmount,
                                    ISNULL(STRING_AGG(pvd.Code, ','), '') as DiscountCode,
                                    SUM(ISNULL(pritems.Surcharge,0)) as Surcharge,
                                    SUM(ISNULL(pritems.Proratadiscount,0)) as Proratadiscount,
                                    SUM(ISNULL(pritems.Transactionfee,0)) as Transactionfee,
                                    CASE 
                                            WHEN EXISTS (
                                                SELECT 1 
                                                FROM SystemSettings
                                                WHERE itemkey  ='ORGANISATION.PAYMENT.EXCLUSIVETRANSACTIONFEESCALCULTION'
                                                  AND Value = 'true'  
                                            )
                                            THEN CAST(1 AS BIT) 
                                            ELSE CAST(0 AS BIT) 
                                        END AS Exclusivetransactionfeescalcultion,
                                    COUNT(*) as ItemsCount,
                                    prd.Currency
                                    ,CONCAT(@siteaddress,'/Report.mvc/GetStandardOutputReport?reportModule=Finance&format=PDF&reportType=',@PAYMENTOUTPUTVERSION,'&reportParameters=DocId|',prd.DocId,';MerchantID|0;') as DownloadPath
                                    FROM PaymentReceipts_Default prd
                                    INNER JOIN [User] ou ON prd.Paymentuserid = ou.userid
                                    INNER JOIN PaymentReceipts_Items pritems ON prd.DocId=pritems.DocId
                                    left join PaymentReceipts_Voucherdiscountevaluations pvd on pvd.Purchaseitemid=pritems.RowId
                                    WHERE    prd.DocId =@DocId 
                                    {merchantFilter} 
                                    {memberFilter}
                                    Group BY prd.DocId,prd.PaymentId,prd.Currency ";

            var paymentSummaryResult = await _readRepository.Value.GetAsync(paymentSummarySQL, cancellationToken, queryParameters, null, "text");
            paymentsummaryVM.PaymentSummary = paymentSummaryResult as PaymentSummary ?? new PaymentSummary();
            return paymentsummaryVM;
        }
    }
}
