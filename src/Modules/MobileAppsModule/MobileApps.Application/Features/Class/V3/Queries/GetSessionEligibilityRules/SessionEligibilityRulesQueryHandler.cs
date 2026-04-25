using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Application.Features.SystemSetting.Queries.GetSystemSettings;
using MobileApps.Domain.Entities.V3.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace MobileApps.Application.Features.Class.V3.Queries.GetSessionEligibilityRules
{
    class SessionEligibilityRulesQueryHandler : IRequestHandler<SessionEligibilityRulesQuery, List<RuleModel>>
    {
        private readonly LazyService<IReadRepository<RawRuleInputDto>> _readRepository;
        private readonly LazyService<IWriteRepository<object>> _writeObjRepository;
        public SessionEligibilityRulesQueryHandler(LazyService<IReadRepository<RawRuleInputDto>> readRepository, LazyService<IWriteRepository<object>> writeObjRepository)
        {
            _readRepository = readRepository;
            _writeObjRepository = writeObjRepository;
        }
        public async Task<List<RuleModel>> Handle(SessionEligibilityRulesQuery request, CancellationToken cancellationToken)
        {
            List<RuleModel> rulesList = new List<RuleModel>();
            string sql = @"SELECT * FROM products_purchaseRules where DocId=@ProductId and Isactive=1";


            var queryParameters = new DynamicParameters();
            queryParameters.Add("@ProductId", request.ProductId);

            var resultList = await _readRepository.Value.GetListAsync(sql, queryParameters, null, "text");
            if (resultList == null) return rulesList;
            foreach (var result in resultList)
            {
                if (result == null) continue;
                var ruleModel = await ParseRuleModelFromJson(result, request.UserId, request.MemberDocId);
                rulesList.Add(ruleModel);
            }

            return rulesList;

        }


        private async Task<RuleModel> ParseRuleModelFromJson(RawRuleInputDto raw, int userId, int memberDocId)
        {

            // Step 1: Parse RuleExpression
            var ruleList = new List<BaseRule>();
            var parts = raw.RuleExpression.Trim('(', ')')
                                           .Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var ruleName = part.Substring(0, part.IndexOf("["));
                var json = part.Substring(part.IndexOf("[") + 1).TrimEnd(']');

                if (ruleName == "GenericGenderRule")
                {
                    var genderRule = JsonConvert.DeserializeObject<GenericGenderRule>(json);
                    genderRule.RuleName = ruleName;
                    ruleList.Add(genderRule);
                }
                else if (ruleName == "GenericAgeRule")
                {
                    var jsonForAge = part.Substring(part.IndexOf("[") + 1).TrimEnd(']');
                    jsonForAge = $"[{jsonForAge}]";
                    bool ageRuleValidate = string.IsNullOrEmpty(json) ? false : await AgeRuleValidate(jsonForAge, userId, memberDocId);
                    var ageRule = new GenericAgeRule();
                    ageRule.RuleName = "GenericAgeRule";
                    ageRule.Name = ageRuleValidate ? "" : "Age Restriction";

                    ruleList.Add(ageRule);
                }
            }

            // Step 2: Return final model
            return new RuleModel
            {
                DocId = raw.DocId,
                RowId = raw.RowId,
                RuleExpression = ruleList,
                From = raw.From,
                To = raw.To,
                Sequence = raw.Sequence,
                Explanation = raw.Explanation
            };
        }

        private async Task<bool> AgeRuleValidate(string json, int userId, int memberDocId)
        {
            string sql = @"StandardRule_AgeRule";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("UserId", userId, dbType: DbType.Int32);
            queryParameters.Add("ForEntityType", "", dbType: DbType.String);
            queryParameters.Add("ForEntityId", memberDocId, dbType: DbType.Int32);
            queryParameters.Add("Arguments", json, dbType: DbType.String);
            queryParameters.Add("IsRuleValid", dbType: DbType.Int32, direction: ParameterDirection.Output);

            // Use ExecuteAsync for SP with output parameters
            var result = await _readRepository.Value.GetAsync(sql, queryParameters, null, "sp");

            int isRuleValid = queryParameters.Get<int>("IsRuleValid");
            return isRuleValid > 0;
        }

    }
}
