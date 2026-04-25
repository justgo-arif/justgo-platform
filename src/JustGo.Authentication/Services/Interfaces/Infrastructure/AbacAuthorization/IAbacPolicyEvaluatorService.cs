using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Infrastructure.AbacAuthorization;
#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Http;

namespace JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization
{
    public interface IAbacPolicyEvaluatorService
    {
        Task<bool> EvaluatePolicyAsync(string policyName, string inputJson, CancellationToken cancellationToken);
        Task<bool> EvaluatePolicyAsync(CancellationToken cancellationToken);
        Task<bool> EvaluatePolicyAsync(string policyName, string action, Dictionary<string, object>? resource, CancellationToken cancellationToken);
        Task<bool> EvaluatePolicyAsync(string policyName, string actionAttribute
            , Dictionary<string, object>? userAttributes, Dictionary<string, object>? resourceAttributes
            , CancellationToken cancellationToken);
        Task<IDictionary<string, UiPermission>> EvaluatePolicyAsync(string policyName, CancellationToken cancellationToken, string? action = null, Dictionary<string, object>? resource = null);
        Task<IDictionary<string, FieldPermission>> EvaluatePolicyMultiAsync(string policyName, CancellationToken cancellationToken, string? action = null, Dictionary<string, object>? resource = null);
        Task<IDictionary<string, FieldPermission>> GetFieldPermissions<T>(T obj, string policyPrefix, Dictionary<string, object>? resource, CancellationToken cancellationToken);
        //Task<IDictionary<string, object>> GetPermissions<T>(string policyName);
        Task<object> EvaluateCombinedPoliciesAsync(string[] policyNames, string[] policyTypes, CancellationToken cancellationToken, string? action = null, Dictionary<string, object>? resource = null);
        Task<object> EvaluateCombinedPoliciesAsync(string[] policyNames, string[] policyTypes, CancellationToken cancellationToken, string[]? actions = null, Dictionary<string, object>[]? resources = null);
        List<string> GetModifiedFields<T1, T2>(T1 newModel, T2 existingModel);
        List<string> GetModifiedFields(Dictionary<string, object> newModel, Dictionary<string, object> existingModel);
        string GetPolicyPrefix(string extensionArea);
    }
}
#endif