using Dapper;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Newtonsoft.Json;
using System.Data;

namespace JustGo.Booking.Application.Features.BookingClasses.Commands.AddPricingChartDiscount
{
    public class AddPricingChartDiscountHandler : IRequestHandler<AddPricingChartDiscountCommand, int>
    {
        private readonly IWriteRepositoryFactory _writeRepositoryFactory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;

        public AddPricingChartDiscountHandler(
            IWriteRepositoryFactory writeRepositoryFactory,
            IUnitOfWork unitOfWork,
            IUtilityService utilityService)
        {
            _writeRepositoryFactory = writeRepositoryFactory;
            _unitOfWork = unitOfWork;
            _utilityService = utilityService;
        }

        public async Task<int> Handle(AddPricingChartDiscountCommand request, CancellationToken cancellationToken = default)
        {
            var repo = _writeRepositoryFactory.GetLazyRepository<object>().Value;
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);
            DateTime createdDate = DateTime.UtcNow;

            // Generate rule expressions
            var (ruleExpression, ruleExpressionDetails) = GenerateRuleExpressions(request);

            // Insert main discount record
            var parameters = new DynamicParameters();
            parameters.Add("@PricingChartDiscountName", request.PricingChartDiscountName);
            parameters.Add("@PricingChartDiscountType", request.PricingChartDiscountType);
            parameters.Add("@PricingChartDiscountValue", request.PricingChartDiscountValue);
            parameters.Add("@PricingChartDiscountStatus", request.PricingChartDiscountStatus);
            parameters.Add("@CreatedDate", createdDate);
            parameters.Add("@CreatedBy", currentUserId);
            parameters.Add("@Ruleexpression", ruleExpression);
            parameters.Add("@RuleexpressionDetails", ruleExpressionDetails);

            var sql = @"
                INSERT INTO JustGoBookingClassPricingChartDiscount
                (PricingChartDiscountName, PricingChartDiscountType, PricingChartDiscountValue, PricingChartDiscountStatus, IsDeleted, CreatedDate, CreatedBy, Ruleexpression, RuleexpressionDetails)
                VALUES (@PricingChartDiscountName, @PricingChartDiscountType, @PricingChartDiscountValue, @PricingChartDiscountStatus, 0, @CreatedDate, @CreatedBy, @Ruleexpression, @RuleexpressionDetails);
                SELECT CAST(SCOPE_IDENTITY() AS INT);
            ";

            var discountId = await repo.ExecuteScalarAsync<int>(sql, cancellationToken, parameters, transaction, "text");

            // Insert discount details
            await InsertDiscountDetailsAsync(repo, request, discountId, cancellationToken, transaction);

            // Handle Products_Discountrules
            //await HandleProductDiscountRulesAsync(repo, request, ruleExpression, ruleExpressionDetails, cancellationToken, transaction);

            // Audit log
            List<dynamic> audits = new List<dynamic>();
            CustomLog.Event(
                        "Class Management|PricingChartDiscount|Created",
                        currentUserId,
                        discountId,
                        EntityType.ClassManagement,
                        -1,
                        "PricingChartDiscount Created;" + JsonConvert.SerializeObject(audits)
                    );

            await _unitOfWork.CommitAsync(transaction);

            return discountId;
        }

        private static (string? ruleExpression, string? ruleExpressionDetails) GenerateRuleExpressions(AddPricingChartDiscountCommand request)
        {
            if (request.PricingChartIds == null || request.PricingChartIds.Count == 0)
                return (null, null);

            var ruleExpressions = new List<string>();
            var rules = new List<List<object>>();

            foreach (var chartId in request.PricingChartIds)
            {
                var ruleDetail = new
                {
                    Rule = "GenericClassBookingRule",
                    Mode = "Member",
                    OwnerId = 0,
                    RuleMode = "PricingCart",
                    EntityId = chartId.ToString(),
                    Clause = "Having",
                    IsShoppingCartChecked = 1
                };

                var ruleForExpression = new
                {
                    Mode = "Member",
                    OwnerId = 0,
                    RuleMode = "PricingCart",
                    EntityId = chartId.ToString(),
                    Clause = "Having",
                    IsShoppingCartChecked = 1
                };

                rules.Add(new List<object> { ruleDetail });
                ruleExpressions.Add($"GenericClassBookingRule[{JsonConvert.SerializeObject(ruleForExpression)}]");
            }

            var ruleExpression = ruleExpressions.Count > 1
                ? $"({string.Join("&&", ruleExpressions)})"
                : ruleExpressions.First();

            var ruleExpressionDetailsObject = new
            {
                Mode = "Standard",
                RuleDescription = $"Pricing Chart Discount - {request.PricingChartDiscountName}",
                RuleGroups = new[]
                {
                    new
                    {
                        GroupName = request.PricingChartDiscountName,
                        Rules = rules
                    }
                }
            };

            var ruleExpressionDetails = JsonConvert.SerializeObject(ruleExpressionDetailsObject);

            return (ruleExpression, ruleExpressionDetails);
        }

        private static async Task InsertDiscountDetailsAsync(
            IWriteRepository<object> repo,
            AddPricingChartDiscountCommand request,
            int discountId,
            CancellationToken cancellationToken,
            IDbTransaction transaction)
        {
            if (request.PricingChartIds == null) return;

            foreach (var chartId in request.PricingChartIds)
            {
                var detailParams = new DynamicParameters();
                detailParams.Add("@PricingChartDiscountId", discountId);
                detailParams.Add("@PricingChartId", chartId);

                var detailSql = @"
                    INSERT INTO JustGoBookingClassPricingChartDiscountDetails
                    (PricingChartDiscountId, PricingChartId, IsDeleted)
                    VALUES (@PricingChartDiscountId, @PricingChartId, 0);
                ";

                await repo.ExecuteAsync(detailSql, cancellationToken, detailParams, transaction, "text");
            }
        }

        //private static async Task HandleProductDiscountRulesAsync(
        //    IWriteRepository<object> repo,
        //    AddPricingChartDiscountCommand request,
        //    string? ruleExpression,
        //    string? ruleExpressionDetails,
        //    CancellationToken cancellationToken,
        //    IDbTransaction transaction)
        //{
        //    if (request.PricingChartIds == null || request.PricingChartIds.Count == 0) return;

        //    // Get matching ProductIds from JustGoBookingClassPricingChartProduct table
        //    var productQueryParams = new DynamicParameters();
        //    productQueryParams.Add("@ChartIds", request.PricingChartIds);

        //    var productQuery = @"
        //        SELECT DISTINCT ProductId 
        //        FROM JustGoBookingClassPricingChartProduct 
        //        WHERE PriceChartId IN @ChartIds
        //    ";

        //    var productIds = await repo.ExecuteQueryAsync<int>(productQuery, cancellationToken, productQueryParams, transaction, "text");

        //    // Insert/Update Products_Discountrules for each ProductId
        //    foreach (var productId in productIds)
        //    {
        //        var productRuleParams = new DynamicParameters();
        //        productRuleParams.Add("@DocId", productId);
        //        productRuleParams.Add("@Explanation", string.Empty);
        //        productRuleParams.Add("@Ispercentage", request.PricingChartDiscountType);
        //        productRuleParams.Add("@Amount", request.PricingChartDiscountValue);
        //        productRuleParams.Add("@Ruleexpression", ruleExpression);
        //        productRuleParams.Add("@FromDate", DateTime.UtcNow.AddYears(-2));
        //        productRuleParams.Add("@ToDate", new DateTime(2099, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        //        productRuleParams.Add("@Sequence", 0);
        //        productRuleParams.Add("@Isactive", 1);
        //        productRuleParams.Add("@Details", ruleExpressionDetails);

        //        var mergeProductRuleSql = @"
        //            MERGE dbo.Products_Discountrules AS target
        //            USING (SELECT @DocId AS DocId) AS source
        //            ON target.DocId = source.DocId
        //            WHEN MATCHED THEN
        //                UPDATE SET 
        //                    Explanation = @Explanation,
        //                    Ispercentage = @Ispercentage,
        //                    Amount = @Amount,
        //                    Ruleexpression = @Ruleexpression,
        //                    [From] = @FromDate,
        //                    [To] = @ToDate,
        //                    Sequence = @Sequence,
        //                    Isactive = @Isactive,
        //                    Details = @Details
        //            WHEN NOT MATCHED THEN
        //                INSERT (DocId, Explanation, Ispercentage, Amount, Ruleexpression, [From], [To], Sequence, Isactive, Details)
        //                VALUES (@DocId, @Explanation, @Ispercentage, @Amount, @Ruleexpression, @FromDate, @ToDate, @Sequence, @Isactive, @Details);
        //        ";

        //        await repo.ExecuteAsync(mergeProductRuleSql, cancellationToken, productRuleParams, transaction, "text");
        //    }
        //}
    }
}