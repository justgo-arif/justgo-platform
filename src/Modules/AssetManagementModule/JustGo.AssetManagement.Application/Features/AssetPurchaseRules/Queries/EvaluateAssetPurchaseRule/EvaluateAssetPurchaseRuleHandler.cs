using AuthModule.Domain.Entities;
using Dapper;
using JustGo.AssetManagement.Application.DTOs.AssetPurchaseRule;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs.LicenseDtos;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetPurchaseRules.Queries.EvaluateAssetPurchaseRule
{
    public class EvaluateAssetPurchaseRuleHandler : IRequestHandler<EvaluateAssetPurchaseRuleQuery, AssetPurchaseRuleResultDTO>
    {
        private readonly IReadRepositoryFactory _readDb;

        public EvaluateAssetPurchaseRuleHandler(IReadRepositoryFactory readDb)
        {
            _readDb = readDb;
        }

        public async Task<AssetPurchaseRuleResultDTO> Handle(EvaluateAssetPurchaseRuleQuery request, CancellationToken cancellationToken)
        {
            string sql = @" select RuleType,Ruleexpression from AssetTypesLicenseLink atl inner join license_links ll on ll.docid = atl.licensedocid 
                                inner join Products_Default pd on pd.docid = ll.entityid 
				                inner join AssetLicensePurchaseRules alpr on alpr.LicenseDocId = atl.LicenseDocId
				                where pd.docid = @ProductId

";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ProductId", request.ProductDocId, DbType.Int32);

            var purchaseRules = await _readDb.GetLazyRepository<AssetPurchaseRuleDTO>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text");

            if (purchaseRules == null || !purchaseRules.Any())
            {
                return new AssetPurchaseRuleResultDTO { IsEligible = true, Reason = "No purchase rule found." };
            }

            bool isEligible = true;
            foreach (var rule in purchaseRules)
            {
                var parsedRules = RuleHelper.AssetPurchaseRuleParser.Parse(rule.RuleExpression);
                isEligible = rule.RuleType.ToLower()=="asset" ? await EvaluatePurchaseRuleForAsset(request.AssetId, parsedRules, cancellationToken) : await EvaluatePurchaseRuleWithAssetOwners(request.AssetId, parsedRules, cancellationToken);
                
                if (!isEligible)
                {
                    return new AssetPurchaseRuleResultDTO
                    {
                        IsEligible = false,
                        Reason = "Not eligible based on purchase rules."
                    };
                }
            }

            return new AssetPurchaseRuleResultDTO
            {
                IsEligible = isEligible,
                Reason = isEligible ? "Eligible for purchase." : "Not eligible based on purchase rules."
            };
        }
        
        /// Checks if the asset owners satisfy the parsed purchase rules.
        private async Task<bool> EvaluatePurchaseRuleWithAssetOwners(int assetId, List<ParsedAssetPurchaseRule> rules, CancellationToken cancellationToken)
        {
            string sql = @"SELECT [User].MemberDocId as Id FROM AssetOwners INNER JOIN [User] ON [User].Userid = AssetOwners.OwnerId WHERE AssetId = @AssetId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@AssetId", assetId, DbType.Int32);

            var owners = await _readDb.GetLazyRepository<InsertedDataIdDTO>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text");

            foreach (var owner in owners)
            {
                foreach (var rule in rules)
                {
                    var procParams = new DynamicParameters();
                    procParams.Add("@IsRuleValid", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    procParams.Add("@UserId", 1, DbType.Int32);
                    procParams.Add("@RuleType", rule.RuleName, DbType.String);
                    procParams.Add("@Arguments", rule.Expression ?? string.Empty, DbType.String);
                    procParams.Add("@ForEntityType", "Member", DbType.String);
                    procParams.Add("@ForEntityId", owner.Id, DbType.Int32);

                    await _readDb.GetLazyRepository<dynamic>().Value.GetAsync(
                        "Rule_Generic",
                        cancellationToken,
                        procParams,
                        null,
                        "sp"
                    );

                    int isRuleValid = procParams.Get<int>("@IsRuleValid");
                    if (isRuleValid == 0)
                        return false; 
                }
            }

            return true;
        }


        private async Task<bool> EvaluatePurchaseRuleForAsset(int assetId, List<ParsedAssetPurchaseRule> rules, CancellationToken cancellationToken)
        {
                foreach (var rule in rules)
                {
                    var procParams = new DynamicParameters();
                    procParams.Add("@IsRuleValid", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    procParams.Add("@UserId", 1, DbType.Int32);
                    procParams.Add("@RuleType", rule.RuleName, DbType.String);
                    procParams.Add("@Arguments", rule.Expression ?? string.Empty, DbType.String);
                    procParams.Add("@ForEntityType", "Asset", DbType.String);
                    procParams.Add("@ForEntityId", assetId, DbType.Int32);

                    await _readDb.GetLazyRepository<dynamic>().Value.GetAsync(
                        "Asset_Rule_Generic",
                        cancellationToken,
                        procParams,
                        null,
                        "sp"
                    );

                    int isRuleValid = procParams.Get<int>("@IsRuleValid");
                    if (isRuleValid == 0)
                        return false;
                }

            return true;
        }

    }
}