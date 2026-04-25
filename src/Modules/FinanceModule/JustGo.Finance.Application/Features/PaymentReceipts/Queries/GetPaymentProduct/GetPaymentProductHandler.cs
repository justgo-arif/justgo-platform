using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Finance.Application.DTOs;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Finance.Application.Features.GetDocIdBySyncGuid;
using JustGo.Finance.Application.Features.MerchantOwner.Queries.GetMerchantId;
using JustGo.Finance.Application.DTOs.ProductDTOs;

namespace JustGo.Finance.Application.Features.PaymentReceipts.Queries.GetPaymentProduct
{
    public class GetPaymentProductHandler : IRequestHandler<GetPaymentProductQuery, PaymentProductVM>
    {
        private readonly LazyService<IReadRepository<Product>> _readRepository;
        private readonly IMediator _mediator;

        public GetPaymentProductHandler(LazyService<IReadRepository<Product>> readRepository, IMediator mediator)
        {
            _readRepository = readRepository;
            _mediator = mediator;
        }

        public async Task<PaymentProductVM> Handle(GetPaymentProductQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SearchText) || request.SearchText == "string") request.SearchText = null;
            var paymentproductVM = new PaymentProductVM();

            string commonConditions = "";
            var docId = await _mediator.Send(new GetDocIdBySyncGuidQuery(request.PaymentId), cancellationToken);
            
            var queryParameters = new DynamicParameters();
            queryParameters.Add("DocId", docId);
            queryParameters.Add("PageNo", request.PageNo);
            queryParameters.Add("PageSize", request.PageSize);

            if (request.MerchantId is not null && request.Source is ProductRequestSource.Merchant)
            {
                var merchantId = await _mediator.Send(new GetMerchantIdQuery(request.MerchantId.Value), cancellationToken);
                queryParameters.Add("MerchantId", merchantId);
                commonConditions += " AND pritems.Merchantid = @MerchantId ";
            }
            if (request.MemberId is not null && request.Source is ProductRequestSource.Member)
            {
                var memberdocid = await _mediator.Send(
               new GetDocIdBySyncGuidQuery(request.MemberId.Value), cancellationToken);
                queryParameters.Add("MemberDocId", memberdocid);
            }
            if (!string.IsNullOrEmpty(request.SearchText))
            {
                queryParameters.Add("SearchText", $"%{request.SearchText}%");
                commonConditions += " AND pd.Name like @SearchText ";
            }
            var productdetailsSQL = @$"select COUNT(Productid) as TotalCount from 
                                    (SELECT Distinct pritems.Productid 
                                    FROM PaymentReceipts_Items pritems
                                    Inner Join Products_Default pd on pritems.Productid = pd.DocId
                                    Where pritems.docid = @DocId 
                                    {commonConditions}) p


                                    select * from (
                                    SELECT  Productid as Id,MIN(RowId) as RowId,ISNULL(Productcode,'') as Code,ISNULL(pd.Name,'') as Name,SUM(Quantity) as Quantity,ISNULL(Price,0) as UnitPrice,
                                    SUM(ISNULL(Surcharge,0)) as Surcharge,SUM(ISNULL(Discount,0)) as Discount,SUM(ISNULL(Tax,0)) as TaxAmount
                                    ,SUM(ISNULL(Gross,0)) as TotalAmount
                                    ,SUM(ISNULL(pritems.Proratadiscount,0)) as Proratadiscount
                                    ,SUM(ISNULL(pritems.Transactionfee,0)) as Transactionfee
                                    ,CASE 
                                            WHEN EXISTS (
                                                SELECT 1 
                                                FROM SystemSettings
                                                WHERE itemkey  ='ORGANISATION.PAYMENT.EXCLUSIVETRANSACTIONFEESCALCULTION'
                                                  AND Value = 'true'  
                                            )
                                            THEN CAST(1 AS BIT) 
                                            ELSE CAST(0 AS BIT) 
                                        END AS   Exclusivetransactionfeescalcultion
                                    ,CASE 
                                        WHEN pd.Location = 'Virtual' OR NULLIF(pd.Location,'') IS NULL THEN '' 
                                        ELSE Concat('store/download?f=',  pd.Location,'&t=repo&p=',pd.DocId,'&p1=&p2=11' )  
                                    END AS ProductImageURL
                                    FROM PaymentReceipts_Items pritems
                                    Inner Join Products_Default pd on pritems.Productid = pd.DocId
                                    Where pritems.docid = @DocId
                                    {commonConditions}
                                    Group By pd.DocId,Productid,ISNULL(Productcode,''),pd.Name,Price,pd.Location
                                    ) pd
                                    Order By RowId
                                    OFFSET (@PageNo - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY
                                      
                                    SELECT   pritems.Productid
                                     ,ISNULL(COALESCE(
                                       NULLIF(CONCAT(u.FirstName , ' ', u.LastName),''),
                                       cd.ClubName,
                                       td.TeamName,
                                       pritems.[Group]
                                       ),'') as MemberName,
                                       ISNULL(COALESCE(
                                       md.MID,
                                       cd.ClubId,
                                       td.TeamID,
                                       pritems.[Group]
                                       ),'') as MemberId,
                                      pritems.Forentityid as MemberDocId
                                    ,CASE 
                                        WHEN ISNULL(u.ProfilePicURL, '') = '' 
                                        THEN ''
                                        ELSE CONCAT(
                                            'Store/download?f=', 
                                            u.ProfilePicURL, 
                                            '&t=user&p=', 
                                            u.UserId
                                        )
                                    END AS ProfilePicURL 
                                    FROM PaymentReceipts_Items pritems 
                                    Inner Join Products_Default pd on pritems.Productid = pd.DocId
                                    Left JOIN [User] u ON pritems.Forentityid = u.MemberDocId
                                    Left Join Members_Default md ON u.MemberDocId = md.DocId
                                    Left JOIN Clubs_Default cd ON pritems.Forentityid = cd.DocId
                                    Left JOIN Teams_Default td ON pritems.Forentityid = td.DocId
                                    Where pritems.docid = @DocId
                                    {commonConditions}";
            await using var productsresult = await _readRepository.Value.GetMultipleQueryAsync(productdetailsSQL, cancellationToken, queryParameters, null, "text");

            int totalCount = (await productsresult.ReadAsync<int>()).FirstOrDefault();
            var products = (await productsresult.ReadAsync<Product>()).ToList();
            var purchasemembers = (await productsresult.ReadAsync<PurchaseMember>()).ToList();
            foreach (var product in products)
            {
                product.members = purchasemembers
                    .Where(r => r.ProductId == product.Id)
                    .Select(r => new PurchaseMember
                    {
                        MemberName = r.MemberName ?? "",
                        MemberId = r.MemberId ?? "",
                        MemberDocId = r.MemberDocId,
                        ProfilePicURL = r.ProfilePicURL ?? "",
                        ProductId = product.Id,
                    }).ToList();
            }
            paymentproductVM.TotalCount = totalCount;
            paymentproductVM.products = products;
            paymentproductVM.PageNo = request.PageNo;
            paymentproductVM.PageSize = request.PageSize;
            return paymentproductVM;
        }
    }
}
