using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs.ExportDTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetMerchantId;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.ExportReceipts
{
    public class ExportReceiptsPagedQueryHandler : IRequestHandler<ExportReceiptsPagedQuery, List<ExportedReceiptDto>>
    {
        private readonly LazyService<IReadRepository<ExportedReceiptDto>> _readRepository;
        private readonly IMediator _mediator;

        public ExportReceiptsPagedQueryHandler(LazyService<IReadRepository<ExportedReceiptDto>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<List<ExportedReceiptDto>> Handle(ExportReceiptsPagedQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SearchText) || request.SearchText == "string") request.SearchText = null;
            if (string.IsNullOrWhiteSpace(request.ScopeKey) || request.ScopeKey == "string") request.ScopeKey = "all";
            if (string.IsNullOrWhiteSpace(request.ColumnName) || request.ColumnName == "string") request.ColumnName = "Field_479";
            if (string.IsNullOrWhiteSpace(request.OrderBy) || request.OrderBy == "string") request.OrderBy = "ASC";
            if (request.PaymentMethods?.Count == 1 && request.PaymentMethods[0] == "string") request.PaymentMethods.Clear();
            if (request.PaymentIds?.Count == 1 && request.PaymentIds[0] == "string") request.PaymentIds.Clear();
            var merchantId = await _mediator.Send(new GetMerchantIdQuery(request.MerchantId), cancellationToken);
           
            string CommonCondition = "";
            string OrderBy = "";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("MerchantId", merchantId);
            queryParameters.Add("PageNo", request.PageNo);
            queryParameters.Add("PageSize", request.PageSize);
            if (request.PaymentIds != null && request.PaymentIds.Any())
            {
                if (!request.PaymentIds.Contains("All"))
                {
                    CommonCondition += " AND prd.[Field_487] IN @PaymentIds ";
                    queryParameters.Add("PaymentIds", request.PaymentIds);
                }
            }

            if (request.PaymentMethods != null && request.PaymentMethods.Any())
            {
                CommonCondition += " AND prd.[Field_494] IN @PaymentMethods ";
                queryParameters.Add("PaymentMethods", request.PaymentMethods);
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
                                OR FORMAT(prd.[Field_479],'dd MMM yyyy, hh:mm tt') LIKE @SearchText 
                                OR  FORMAT(prd.[Field_479],'dd/MM/yyyy') like @SearchText )
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
                OrderBy = "prd.DocId ASC";
            }

            var sql = @$"  SELECT  prd.[Field_487] As PaymentId,
                        CONCAT( u.FirstName,' ',u.LastName) AS CustomerName,
                        u.MemberId as CustomerId,
                        prd.[Field_494] As PaymentMethod, prbr.Amount AS TotalAmount,
						CASE
                        WHEN prd.[Field_487] LIKE '%RR%' THEN 'Refund'
                        ELSE 'Payment'
                        END AS PaymentType,
                        prd.[Field_495] As ReceiptStatus,
                        FORMAT(prd.[Field_479],'dd MMM yyyy, hh:mm tt') as PaymentDate,
                        prd.[Field_488] As Products
                        FROM PaymentReceipts_Breakdownbyrecipient prbr 
                        INNER JOIN Document_12_72 prd ON prd.DocId = prbr.DocId
                        INNER JOIN ProcessInfo pri ON pri.PrimaryDocId = prd.DocId
                        INNER JOIN [State] st ON st.StateId = pri.CurrentStateId
                        INNER JOIN [User] u ON u.Userid = prd.[Field_478]
                        WHERE prbr.Marchentid = @MerchantId {CommonCondition}
                         Order By {OrderBy}
                        OFFSET (@PageNo - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY";

            var payments = (await _readRepository.Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();


            return payments ?? new List<ExportedReceiptDto>();
        }
    }
}
