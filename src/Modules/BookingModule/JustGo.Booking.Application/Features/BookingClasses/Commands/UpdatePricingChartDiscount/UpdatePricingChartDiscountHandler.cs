using Dapper;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using Newtonsoft.Json;
using System.Data;

namespace JustGo.Booking.Application.Features.BookingClasses.Commands.UpdatePricingChartDiscount
{
    public class UpdatePricingChartDiscountHandler : IRequestHandler<UpdatePricingChartDiscountCommand, int>
    {
        private readonly IWriteRepositoryFactory _writeRepositoryFactory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUtilityService _utilityService;

        public UpdatePricingChartDiscountHandler(
            IWriteRepositoryFactory writeRepositoryFactory,
            IUnitOfWork unitOfWork,
            IUtilityService utilityService)
        {
            _writeRepositoryFactory = writeRepositoryFactory;
            _unitOfWork = unitOfWork;
            _utilityService = utilityService;
        }

        public async Task<int> Handle(UpdatePricingChartDiscountCommand request, CancellationToken cancellationToken = default)
        {
            var repo = _writeRepositoryFactory.GetLazyRepository<object>().Value;
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            int currentUserId = await _utilityService.GetCurrentUserId(cancellationToken);

            // Clean up old product discount rules
            // await CleanupOldProductDiscountRulesAsync(repo, request, cancellationToken, transaction);

            // Generate rule expressions
            var (ruleExpression, ruleExpressionDetails) = GenerateRuleExpressions(request);

            // Update main discount record
            var totalAffected = await UpdateMainDiscountRecordAsync(repo, request, ruleExpression, ruleExpressionDetails, cancellationToken, transaction);

            // Update discount details
            await UpdateDiscountDetailsAsync(repo, request, cancellationToken, transaction);

            // Update/Insert product discount rules
            //if (request.PricingChartIds != null)
            //{
            //    await UpdateProductDiscountRulesAsync(repo, request, ruleExpression, ruleExpressionDetails, cancellationToken, transaction);
            //}

            // Audit log
            List<dynamic> audits = new List<dynamic>();
            CustomLog.Event(
                        "Class Management|PricingChartDiscount|Updated",
                        currentUserId,
                        request.PricingChartDiscountId,
                        EntityType.ClassManagement,
                        -1,
                        "PricingChartDiscount Update;" + JsonConvert.SerializeObject(audits)
                    );

            await _unitOfWork.CommitAsync(transaction);
            return totalAffected;
        }

        //private static async Task CleanupOldProductDiscountRulesAsync(
        //    IWriteRepository<object> repo,
        //    UpdatePricingChartDiscountCommand request,
        //    CancellationToken cancellationToken,
        //    IDbTransaction transaction)
        //{
        //    // Get old PricingChartIds
        //    var getOldChartIdsParams = new DynamicParameters();
        //    getOldChartIdsParams.Add("@PricingChartDiscountId", request.PricingChartDiscountId);

        //    var getOldChartIdsSql = @"
        //        SELECT PricingChartId 
        //        FROM JustGoBookingClassPricingChartDiscountDetails
        //        WHERE PricingChartDiscountId = @PricingChartDiscountId
        //    ";

        //    var oldPricingChartIds = await repo.ExecuteQueryAsync<int>(getOldChartIdsSql, cancellationToken, getOldChartIdsParams, transaction, "text");
        //    var oldPricingChartIdsList = oldPricingChartIds.ToList();

        //    // Get removed PricingChartIds
        //    var newPricingChartIds = request.PricingChartIds ?? new List<int>();
        //    var removedPricingChartIds = oldPricingChartIdsList.Except(newPricingChartIds).ToList();

        //    if (!removedPricingChartIds.Any()) return;

        //    // Get ProductIds for removed PricingChartIds
        //    var oldProductQueryParams = new DynamicParameters();
        //    oldProductQueryParams.Add("@RemovedChartIds", removedPricingChartIds);

        //    var oldProductQuery = @"
        //        SELECT DISTINCT ProductId 
        //        FROM JustGoBookingClassPricingChartProduct 
        //        WHERE PriceChartId IN @RemovedChartIds
        //    ";

        //    var oldProductIds = await repo.ExecuteQueryAsync<int>(oldProductQuery, cancellationToken, oldProductQueryParams, transaction, "text");

        //    // Delete old product discount rules
        //    foreach (var oldProductId in oldProductIds)
        //    {
        //        var deleteParams = new DynamicParameters();
        //        deleteParams.Add("@DocId", oldProductId);

        //        var deleteProductRuleSql = @"
        //            DELETE FROM dbo.Products_Discountrules 
        //            WHERE DocId = @DocId
        //        ";

        //        await repo.ExecuteAsync(deleteProductRuleSql, cancellationToken, deleteParams, transaction, "text");
        //    }
        //}

        private static (string? ruleExpression, string? ruleExpressionDetails) GenerateRuleExpressions(UpdatePricingChartDiscountCommand request)
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

        private static async Task<int> UpdateMainDiscountRecordAsync(
            IWriteRepository<object> repo,
            UpdatePricingChartDiscountCommand request,
            string? ruleExpression,
            string? ruleExpressionDetails,
            CancellationToken cancellationToken,
            IDbTransaction transaction)
        {
            var updateSql = @"
                UPDATE JustGoBookingClassPricingChartDiscount
                SET PricingChartDiscountName = @PricingChartDiscountName,
                    PricingChartDiscountType = @PricingChartDiscountType,
                    PricingChartDiscountValue = @PricingChartDiscountValue,
                    Ruleexpression = @Ruleexpression,
                    RuleexpressionDetails = @RuleexpressionDetails
                WHERE PricingChartDiscountId = @PricingChartDiscountId AND IsDeleted = 0;
            ";

            var parameters = new DynamicParameters();
            parameters.Add("@PricingChartDiscountId", request.PricingChartDiscountId, DbType.Int32);
            parameters.Add("@PricingChartDiscountName", request.PricingChartDiscountName);
            parameters.Add("@PricingChartDiscountType", request.PricingChartDiscountType);
            parameters.Add("@PricingChartDiscountValue", request.PricingChartDiscountValue);
            parameters.Add("@Ruleexpression", ruleExpression);
            parameters.Add("@RuleexpressionDetails", ruleExpressionDetails);

            return await repo.ExecuteAsync(updateSql, cancellationToken, parameters, transaction, "Text");
        }

        private static async Task UpdateDiscountDetailsAsync(
            IWriteRepository<object> repo,
            UpdatePricingChartDiscountCommand request,
            CancellationToken cancellationToken,
            IDbTransaction transaction)
        {
            // Delete old details
            var deleteDetailsParams = new DynamicParameters();
            deleteDetailsParams.Add("@PricingChartDiscountId", request.PricingChartDiscountId);

            var deleteDetailsSql = @"
                DELETE FROM JustGoBookingClassPricingChartDiscountDetails
                WHERE PricingChartDiscountId = @PricingChartDiscountId;
            ";
            await repo.ExecuteAsync(deleteDetailsSql, cancellationToken, deleteDetailsParams, transaction, "Text");

            // Insert new details
            if (request.PricingChartIds != null)
            {
                foreach (var chartId in request.PricingChartIds)
                {
                    var detailParams = new DynamicParameters();
                    detailParams.Add("@PricingChartDiscountId", request.PricingChartDiscountId);
                    detailParams.Add("@PricingChartId", chartId);

                    var detailSql = @"
                        INSERT INTO JustGoBookingClassPricingChartDiscountDetails
                        (PricingChartDiscountId, PricingChartId, IsDeleted)
                        VALUES (@PricingChartDiscountId, @PricingChartId, 0);
                    ";

                    await repo.ExecuteAsync(detailSql, cancellationToken, detailParams, transaction, "Text");
                }
            }
        }

        //private static async Task UpdateProductDiscountRulesAsync(
        //    IWriteRepository<object> repo,
        //    UpdatePricingChartDiscountCommand request,
        //    string? ruleExpression,
        //    string? ruleExpressionDetails,
        //    CancellationToken cancellationToken,
        //    IDbTransaction transaction)
        //{
        //    var productQueryParams = new DynamicParameters();
        //    productQueryParams.Add("@ChartIds", request.PricingChartIds);

        //    var productQuery = @"
        //        SELECT DISTINCT ProductId 
        //        FROM JustGoBookingClassPricingChartProduct 
        //        WHERE PriceChartId IN @ChartIds
        //    ";

        //    var productIds = await repo.ExecuteQueryAsync<int>(productQuery, cancellationToken, productQueryParams, transaction, "text");

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