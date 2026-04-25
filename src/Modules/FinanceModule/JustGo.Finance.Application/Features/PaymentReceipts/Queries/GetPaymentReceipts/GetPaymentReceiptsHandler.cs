using System.Data;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetMerchantId;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentReceipts
{
    public class GetPaymentReceiptsHandler : IRequestHandler<GetPaymentReceiptsQuery, PaymentInfoVM>
    {
        private readonly LazyService<IReadRepository<PaymentInfoDto>> _readRepository;
        private readonly IMediator _mediator; 
        public GetPaymentReceiptsHandler(LazyService<IReadRepository<PaymentInfoDto>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<PaymentInfoVM> Handle(GetPaymentReceiptsQuery request, CancellationToken cancellationToken)
        {
            var paymentInfoVM = new PaymentInfoVM();
            string CommonCondition = "";
            string OrderBy = "";
            if (string.IsNullOrWhiteSpace(request.SearchText) || request.SearchText == "string") request.SearchText = null;
            if (string.IsNullOrWhiteSpace(request.ScopeKey) || request.ScopeKey == "string") request.ScopeKey = "all";
            if (string.IsNullOrWhiteSpace(request.ColumnName) || request.ColumnName == "string") request.ColumnName = "Field_479";
            if (string.IsNullOrWhiteSpace(request.OrderBy) || request.OrderBy == "string") request.OrderBy = "ASC";
            if (request.PaymentMethods?.Count == 1 && request.PaymentMethods[0] == "string") request.PaymentMethods.Clear();
            if (request.CustomerIds?.Count == 1 && request.CustomerIds[0] == "string") request.CustomerIds.Clear();
            var merchantId = await _mediator.Send(new GetMerchantIdQuery(request.MerchantId), cancellationToken);

            var queryParameters = new DynamicParameters();
            queryParameters.Add("MerchantId", merchantId);
            queryParameters.Add("PageNo", request.PageNo);
            queryParameters.Add("PageSize", request.PageSize);



            if (request.PaymentMethods != null && request.PaymentMethods.Any())
            {
                CommonCondition += " AND prd.[Field_494] IN @PaymentMethods ";
                queryParameters.Add("PaymentMethods", request.PaymentMethods);
            }

            if (request.CustomerIds != null && request.CustomerIds.Any())
            {
                CommonCondition += " AND u.UserSyncId IN @CustomerIds ";
                queryParameters.Add("CustomerIds", request.CustomerIds);
            }

            if (request.StatusIds != null && request.StatusIds.Any(x => x > 0))
            {
                var validStatusIds = request.StatusIds.Where(x => x > 0).ToList();
                CommonCondition += " AND st.StateId IN @StatusIds ";
                queryParameters.Add("StatusIds", validStatusIds);
            }
            if (request.FromDate.HasValue && request.ToDate.HasValue)
            {
                var from = request.FromDate.Value.Date;
                var to = request.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                CommonCondition += " AND prd.[Field_479] BETWEEN @FromDate AND @ToDate ";
                queryParameters.Add("FromDate", from);
                queryParameters.Add("ToDate", to);
            }
            if (!string.IsNullOrEmpty(request.SearchText) && !string.IsNullOrEmpty(request.ScopeKey))
            {
                CommonCondition += @$" AND (
                            (
                                @ScopeKey = 'all' 
                                AND ( prd.[Field_487] LIKE @SearchText 
                                OR CONCAT( u.FirstName,' ',u.LastName) LIKE @SearchText  
                                OR u.MemberId LIKE @SearchText 
                                OR FORMAT(prd.[Field_479],'dd MMM yyyy, hh:mm tt') LIKE @SearchText )
                            )
                            OR
                            (
                                @ScopeKey = 'paymentid' 
                                AND prd.[Field_487] LIKE @SearchText 
                            )
                            OR
                            (
                                @ScopeKey = 'customername' 
                                AND CONCAT( u.FirstName,' ',u.LastName) LIKE @SearchText 
                            )
                            OR
                            (
                                @ScopeKey = 'customermemberid' 
                                AND u.MemberId LIKE @SearchText 
                            )
                            OR
                            (
                                @ScopeKey = 'paymentdate' 
                                AND ( FORMAT(prd.[Field_479],'dd MMM yyyy, hh:mm tt') LIKE @SearchText OR  FORMAT(prd.[Field_479],'dd/MM/yyyy') like @SearchText )
                            )
                        ) ";
                queryParameters.Add("ScopeKey", request.ScopeKey.ToLower());
                queryParameters.Add("SearchText", $"%{request.SearchText}%");
            }
            var columnMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                {
                                    { "PaymentId", "Field_487" },
                                    { "PaymentMethod", "Field_494" },
                                    { "Date", "Field_479" }
                                };

            var allowedOrders = new[] { "ASC", "DESC" };

            if (!string.IsNullOrEmpty(request?.ColumnName) &&
                columnMappings.ContainsKey(request.ColumnName) &&
                allowedOrders.Contains(request.OrderBy?.ToUpper()))
            {
                var backendColumn = columnMappings[request.ColumnName];
                OrderBy = $"prd.{backendColumn} {request.OrderBy?.ToUpper()}";
            }
            else
            {
                OrderBy = "prd.Field_479 DESC";
            }

            var sql = @$" Declare @Payments  TABLE  (
                            DocId INT,
                            PaymentId NVARCHAR(MAX),
                            CustomerName NVARCHAR(MAX),
                            CustomerId NVARCHAR(MAX),
                            CustomerEmailAddress NVARCHAR(MAX),
                            PaymentMethod NVARCHAR(50),
                            TotalAmount DECIMAL(18, 2),
                            TransactionFee DECIMAL(18, 2),
                            PaymentDate NVARCHAR(MAX),
                            PaymentPaidTime NVARCHAR(MAX),
                            ReceiptStatus NVARCHAR(50),
                            StatusId INT,
                            Status NVARCHAR(50),
                            Currency NVARCHAR(10),
                            ExchangeRate DECIMAL(18, 6),
                            PaymentType NVARCHAR(50),
                            Description NVARCHAR(MAX),
                            ProfilePicURL NVARCHAR(MAX) 
                        );
                        INSERT INTO @Payments
                       SELECT  prd.DocId,prd.[Field_487] As PaymentId,
                        CONCAT( u.FirstName,' ',u.LastName) AS CustomerName,
                        u.MemberId as CustomerId,
                        ISNULL(u.EmailAddress,'') as CustomerEmailAddress,
                        ISNULL(prd.[Field_494],'') As PaymentMethod, ISNULL(prbr.Amount,0) AS TotalAmount,
                        ISNULL(prbr.TransactionFee,0) as TransactionFee ,
                        FORMAT(prd.[Field_479],'dd MMM yyyy, hh:mm tt') as PaymentDate,
                        ISNULL(prd.[Field_2487],'00:00') as Paymentpaidtime,
                        ISNULL(prd.[Field_495],'') As ReceiptStatus, pri.CurrentStateId AS StatusId,
                        ISNULL(st.Name,'') AS [Status], ISNULL(prd.[Field_2287],'') As Currency, ISNULL(prd.[Field_2288],0) As Exchangerate,
                        CASE
                        WHEN prd.[Field_494] ='Stripe' THEN 'Autometic'
                        ELSE 'Manual'
                        END AS PaymentType,
                        ISNULL(prd.[Field_488],'') As Description,
                        ISNULL(u.ProfilePicURL,'') as ProfilePicURL
                        FROM PaymentReceipts_Breakdownbyrecipient prbr 
                        INNER JOIN Document_12_72 prd ON prd.DocId = prbr.DocId
                        INNER JOIN ProcessInfo pri ON pri.PrimaryDocId = prd.DocId
                        INNER JOIN [State] st ON st.StateId = pri.CurrentStateId
                        INNER JOIN [User] u ON u.Userid = prd.[Field_478]
                        WHERE prbr.Marchentid = @MerchantId AND ISNULL(prd.Field_3715,0)<=0 {CommonCondition}
                                                Order By {OrderBy}
                        OFFSET (@PageNo - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY
                        select d.SyncGuid as Id,p.* from @Payments p
                        INNER JOIN [Document][d] ON p.DocId = d.DocId ";
            if ((request?.TotalCount ?? 0) <= 0)
            {

                var sqlCount = @$" SELECT   
                            Count(prd.DocId) as TotalCount
                        FROM PaymentReceipts_Breakdownbyrecipient prbr 
                        INNER JOIN Document_12_72 prd ON prd.DocId = prbr.DocId
                        INNER JOIN ProcessInfo pri ON pri.PrimaryDocId = prd.DocId
                        INNER JOIN [State] st ON st.StateId = pri.CurrentStateId
                        INNER JOIN [User] u ON u.Userid = prd.[Field_478]
                        WHERE prbr.Marchentid = @MerchantId AND ISNULL(prd.Field_3715,0)<=0 {CommonCondition} ";
                var totalCountObj = await _readRepository.Value.GetSingleAsync(sqlCount, cancellationToken, queryParameters, null, "text");
                paymentInfoVM.TotalCount = totalCountObj is not null ? Convert.ToInt32(totalCountObj) : 0;
            }
            else
            {
                paymentInfoVM.TotalCount = request?.TotalCount;
            }


            paymentInfoVM.payments = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();

            paymentInfoVM.PageNo = request!.PageNo;
            paymentInfoVM.PageSize = request.PageSize;

            return paymentInfoVM;
        }
    }
}
