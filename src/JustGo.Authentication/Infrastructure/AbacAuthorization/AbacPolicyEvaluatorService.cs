using Dapper;
using JustGo.Authentication.Infrastructure.Caching;
using JustGo.Authentication.Infrastructure.JwtAuthentication;
using JustGo.Authentication.Services.Interfaces.Infrastructure.AbacAuthorization;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Caching;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using System.Data;
using System.Reflection;
using System.Text.Json;
#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Interop;
using OpaDotNet.Wasm;
using static JustGo.Authentication.Infrastructure.Logging.AuditScheme.EmailManagement;

namespace JustGo.Authentication.Infrastructure.AbacAuthorization
{
    public class AbacPolicyEvaluatorService : IAbacPolicyEvaluatorService
    {
        private readonly IAbacPolicyService _abacPolicyService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IAbacPolicyExtensionService _abacPolicyExtensionService;
        private readonly IHybridCacheService _cache;
        public AbacPolicyEvaluatorService(IAbacPolicyService abacPolicyService, IHttpContextAccessor httpContextAccessor
            , IReadRepositoryFactory readRepository
            , IAbacPolicyExtensionService abacPolicyExtensionService
            , IHybridCacheService cache)
        {
            _abacPolicyService = abacPolicyService;
            _httpContextAccessor = httpContextAccessor;
            _readRepository = readRepository;
            _abacPolicyExtensionService = abacPolicyExtensionService;
            _cache = cache;
        }

        public async Task<bool> EvaluatePolicyAsync(string policyName, string inputJson, CancellationToken cancellationToken)
        {
            var policyStream = await GetCompiledPolicyAsync(policyName, cancellationToken);
            if (policyStream is null)
            {
                return true;
            }
            policyStream.Position = 0;
            if (policyStream.Length == 0)
                return true;
            using var engine = OpaBundleEvaluatorFactory.Create(policyStream);

            var policyResult = engine.EvaluateRaw(inputJson);
            var result = JsonSerializer.Deserialize<JsonElement>(policyResult)[0].GetProperty("result").GetBoolean();
            return result;
        }

        public async Task<bool> EvaluatePolicyAsync(CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;
            var attributeParams = context?.GetEndpoint()?.Metadata.GetMetadata<CustomAuthorizeAttribute>();
            var action = attributeParams?.Action ?? string.Empty;
            var policyName = attributeParams?.PolicyName;
            if (context?.User.Identity is null || !context.User.Identity.IsAuthenticated)
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(policyName))
            {
                return true;
            }
            var input = await GetPolicyInputAsync(policyName, action, cancellationToken);
            var policyStream = await GetCompiledPolicyAsync(policyName, cancellationToken);

            if (policyStream is null)
                return true;
            policyStream.Position = 0;
            if (policyStream.Length == 0)
                return true;
            using var engine = OpaBundleEvaluatorFactory.Create(policyStream);
            //var jss = JsonSerializer.Serialize(input);
            var policyResult = engine.EvaluatePredicate(input);
            return policyResult.Result;
        }
        public async Task<bool> EvaluatePolicyAsync(string policyName, string action, Dictionary<string, object>? resource, CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.User.Identity is null || !context.User.Identity.IsAuthenticated)
            {
                return false;
            }
            var input = await GetPolicyInputAsync(policyName, action, cancellationToken, resource);

            var policyStream = await GetCompiledPolicyAsync(policyName, cancellationToken);

            if (policyStream is null)
                return true;
            policyStream.Position = 0;
            if (policyStream.Length == 0)
                return true;
            using var engine = OpaBundleEvaluatorFactory.Create(policyStream);

            var policyResult = engine.EvaluatePredicate(input);
            return policyResult.Result;
        }
        public async Task<bool> EvaluatePolicyAsync(string policyName, string actionAttribute
            , Dictionary<string, object>? userAttributes, Dictionary<string, object>? resourceAttributes
            , CancellationToken cancellationToken)
        {
            var input = new Dictionary<string, object>();
            //Action Attribute
            if (!string.IsNullOrWhiteSpace(actionAttribute))
            {
                input["action"] = actionAttribute;
            }

            //User Attribute
            if (userAttributes is not null)
            {
                input["user"] = userAttributes;
            }

            //Resource Attribute
            if (resourceAttributes is not null)
            {
                input["resource"] = resourceAttributes;
            }

            var policyStream = await GetCompiledPolicyAsync(policyName, cancellationToken);

            if (policyStream is null)
                return true;
            policyStream.Position = 0;
            if (policyStream.Length == 0)
                return true;
            using var engine = OpaBundleEvaluatorFactory.Create(policyStream);

            var policyResult = engine.EvaluatePredicate(input);
            return policyResult.Result;
        }
        public async Task<IDictionary<string, UiPermission>> EvaluatePolicyAsync(string policyName, CancellationToken cancellationToken, string? action = null, Dictionary<string, object>? resource = null)
        {
            var input = await GetPolicyInputAsync(policyName, action, cancellationToken, resource);
            var policyStream = await GetCompiledPolicyAsync(policyName, cancellationToken);

            if (policyStream is null)
                return new Dictionary<string, UiPermission>();
            policyStream.Position = 0;
            if (policyStream.Length == 0)
                return new Dictionary<string, UiPermission>();

            using var engine = OpaBundleEvaluatorFactory.Create(policyStream);

            var policyResult = engine.Evaluate<IDictionary<string, object>, IDictionary<string, UiPermission>>(input).Result;
            return policyResult;
        }
        public async Task<IDictionary<string, FieldPermission>> EvaluatePolicyMultiAsync(string policyName, CancellationToken cancellationToken, string? action = null, Dictionary<string, object>? resource = null)
        {
            var input = await GetPolicyInputAsync(policyName, action, cancellationToken, resource);

            var policyStream = await GetCompiledPolicyAsync(policyName, cancellationToken);

            if (policyStream is null)
            {
                return new Dictionary<string, FieldPermission>
                {
                    { "default", CreateDefaultPermissions() }
                };
            }
            policyStream.Position = 0;
            if (policyStream.Length == 0)
            {
                return new Dictionary<string, FieldPermission>
                {
                    { "default", CreateDefaultPermissions() }
                };
            }
            using var engine = OpaBundleEvaluatorFactory.Create(policyStream);

            var policyResult = engine.Evaluate<IDictionary<string, object>, IDictionary<string, FieldPermission>>(input).Result;
            return policyResult;
        }
        public FieldPermission CreateDefaultPermissions(bool defaultValue = true)
        {
            var permission = new FieldPermission();
            var properties = typeof(FieldPermission).GetProperties();

            foreach (var prop in properties)
            {
                if (prop.CanWrite && prop.PropertyType == typeof(bool))
                {
                    prop.SetValue(permission, defaultValue);
                }
            }

            return permission;
        }
        private async Task<Dictionary<string, object>> GetPolicyInputAsync(string policyName, string? action, CancellationToken cancellationToken, Dictionary<string, object>? additionalResource = null)
        {
            var input = new Dictionary<string, object>();
            //Action Attribute
            if (action is not null)
            {
                input["action"] = action;
            }

            //User Attribute
            input["user"] = await GetUserAttributes(cancellationToken);

            //Resource Attribute
            var resourceAttributes = await GetResourceAttributes(cancellationToken);
            if (additionalResource is not null)
            {
                foreach (var kvp in additionalResource)
                {
                    resourceAttributes[kvp.Key] = kvp.Value;
                }
            }
            input["resource"] = resourceAttributes;

            //Environment Attribute
            input["environment"] = GetEnvironmentAttributes();

            //Policy extension
            await GetPolicyExtensionResourceAttributes(
                policyName,
                resourceAttributes ?? new Dictionary<string, object>(),
                input,
                cancellationToken
            );

            return input;
        }
        private async Task<Stream?> GetCompiledPolicyAsync(string policyName, CancellationToken cancellationToken)
        {
            var cacheKey = $"compiled_policy_{policyName}";
            var compiledPolicyBytes = await _cache.GetOrSetAsync<byte[]?>(
                cacheKey,
                async ct =>
                {
                    //var filePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"../Modules/AuthModule/AuthModule.Infrastructure/Infrastructure/RegoPolicy/Policies"));
                    //var path = Path.Combine(filePath, "policy.rego");
                    //var policyStream = await compiler.CompileFile(path, new[] { "allowCredentialAdd/allow" });
                    var policy = await _abacPolicyService.GetPolicyByName(policyName, cancellationToken);
                    if (policy is null)
                        return null;

                    var compiler = new RegoInteropCompiler();
                    var policyEntryPoint = $"{policyName}/{policy.PolicyEntryPoint}";
                    using var policyStream = await compiler.CompileSourceAsync(policy.PolicyRule, new() { Entrypoints = [policyEntryPoint] });
                    if (policyStream != null)
                    {
                        using var memoryStream = new MemoryStream();
                        await policyStream.CopyToAsync(memoryStream, ct);
                        return memoryStream.ToArray();
                    }

                    return null;
                },
                TimeSpan.FromMinutes(30),
                new[] { CacheTag.ABAC.ToString() },
                cancellationToken);
            return compiledPolicyBytes != null ? new MemoryStream(compiledPolicyBytes) : null;
        }
        private async Task<IDictionary<string, object>> GetUserAttributes(CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;
            var userAttribute = new Dictionary<string, object>();

            var claims = context?.User.Claims.ToList();
            var userSyncId = claims?.FirstOrDefault(c => c.Type == "UserSyncId")?.Value ?? string.Empty;
            var roles = claims?.Where(w => w.Type == "role").Select(s => s.Value).ToList() ?? new List<string>();
            var metaRoles = claims?.Where(w => w.Type == "meta_role").Select(s => s.Value).ToList() ?? new List<string>();
            var abacRoles = claims?.Where(w => w.Type == "abac_role").Select(s => s.Value).ToList() ?? new List<string>();
            var clubsIn = claims?.Where(w => w.Type == "clubs_in").Select(s => s.Value).ToList() ?? new List<string>();
            var familyMembers = claims?.Where(w => w.Type == "family_members").Select(s => s.Value).ToList() ?? new List<string>();

            var user = await GetUserByUserSyncId(userSyncId, cancellationToken);
            if (clubsIn.Count == 0)
            {
                clubsIn = await GetClubsByUserId(user.Userid, cancellationToken);
            }
            var clubsAdminOfWithChild = await GetAdminClubsWithChildByUserIdQuery(user.Userid, cancellationToken);

            string? organizationId = null;
            if (context?.Request.Query != null && context.Request.Query.TryGetValue("organizationId", out var orgIdValue))
            {
                organizationId = orgIdValue.ToString();
                abacRoles = await GetAbacRolesByUserIdAndOrganization(user.Userid, organizationId, cancellationToken);
            }

            var permissions = await GetPermissionsByUserIdQuery(user.Userid, cancellationToken);

            userAttribute["loginId"] = user.LoginId.ToLowerInvariant();
            userAttribute["id"] = userSyncId.ToLowerInvariant();
            userAttribute["memberId"] = userSyncId.ToLowerInvariant();
            userAttribute["roles"] = roles;
            userAttribute["metaRoles"] = metaRoles;
            userAttribute["abacRoles"] = abacRoles;
            userAttribute["clubsIn"] = clubsIn;
            userAttribute["familyMembers"] = familyMembers;
            userAttribute["clubs"] = clubsAdminOfWithChild;
            userAttribute["permissions"] = permissions;

            return userAttribute;
        }

        private async Task<List<string>> GetAbacRolesByUserIdAndOrganization(int userId, string organizationId, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(organizationId, out var syncGuid))
            {
                return new List<string>();
            }

            string sql = @"
                            DECLARE @ClubDocId INT;
        
                            SELECT @ClubDocId = d.DocId FROM [dbo].[Document] d 
                            LEFT JOIN [dbo].[Hierarchies] h ON h.EntityId = d.DocId WHERE d.SyncGuid = @OrganizationSyncGuid;
        
                            SELECT DISTINCT ar.[Name] FROM [dbo].[AbacRoles] ar
                            INNER JOIN [dbo].[AbacUserRoles] aur ON ar.[Id] = aur.[RoleId] WHERE aur.[UserId] = @UserId 
                            AND (aur.[OrganizationId] = @ClubDocId OR aur.[OrganizationId] = 0)";

            //--AND aur.[OrganizationId] = @ClubDocId";

            //var queryParameters = new DynamicParameters();
            //queryParameters.Add("@UserId", userId);
            //queryParameters.Add("@OrganizationSyncGuid", syncGuid, DbType.Guid);

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", userId);
            queryParameters.Add("@OrganizationSyncGuid", syncGuid, DbType.Guid);

            var result = (await _readRepository.GetLazyRepository<dynamic>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList();

            return result.Select(r => (string)r.Name).ToList();
        }
        private async Task<IDictionary<string, object>> GetResourceAttributes(CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;
            var attributeParams = context?.GetEndpoint()?.Metadata.GetMetadata<CustomAuthorizeAttribute>();
            var policyName = attributeParams?.PolicyName ?? string.Empty;

            var resource = new Dictionary<string, object>();
            var requiredFields = attributeParams?.RequiredFields;

            var routeData = context?.GetRouteData();
            if (routeData?.Values != null && requiredFields != null)
            {
                foreach (var param in routeData.Values)
                {
                    if (requiredFields.Contains(param.Key))
                    {
                        resource[param.Key] = param.Value?.ToString()?.ToLowerInvariant() ?? string.Empty;
                    }
                }
            }

            if (context != null && context.Request != null && requiredFields != null && context.Request.Query != null)
            {
                foreach (var queryParam in context.Request.Query)
                {
                    if (requiredFields.Contains(queryParam.Key))
                    {
                        resource[queryParam.Key] = queryParam.Value.ToString().ToLowerInvariant();
                    }
                }
            }

            if (context != null && context.Request != null && context.Request.Method != HttpMethods.Get && context.Request.ContentLength > 0)
            {
                // Handle multipart/form-data (file uploads) - read form fields, ignore files
                if (context.Request.HasFormContentType)
                {
                    var form = await context.Request.ReadFormAsync(cancellationToken);
                    if (requiredFields != null)
                    {
                        foreach (var field in requiredFields)
                        {
                            if (form.TryGetValue(field, out var value))
                            {
                                resource[field] = value.ToString().ToLowerInvariant();
                            }
                        }
                    }
                }
                // Handle Normal Json
                else
                {
                    context.Request.EnableBuffering();
                    using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                    var requestBody = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;

                    if (!string.IsNullOrWhiteSpace(requestBody))
                    {
                        var bodyJson = JsonSerializer.Deserialize<IDictionary<string, object>>(requestBody);
                        if (bodyJson != null && requiredFields != null)
                        {
                            foreach (var field in requiredFields)
                            {
                                if (bodyJson.TryGetValue(field, out var value))
                                    resource[field] = value?.ToString() ?? string.Empty;
                            }
                        }
                    }
                }
            }
            return resource;
        }
        private IDictionary<string, object> GetEnvironmentAttributes()
        {
            var context = _httpContextAccessor.HttpContext;

            return new Dictionary<string, object>
            {
                ["currentTime"] = DateTime.UtcNow
            };
        }
        private async Task GetPolicyExtensionResourceAttributes(string policyName, IDictionary<string, object> resource, IDictionary<string, object> context, CancellationToken cancellationToken)
        {
            var policyExtensions = await _abacPolicyExtensionService.GetPolicyExtensionByPolicyName(policyName, cancellationToken);
            foreach (var policyExtension in policyExtensions)
            {
                var queryParameters = new DynamicParameters();
                if (!string.IsNullOrWhiteSpace(policyExtension.SqlParams))
                {
                    var parameters = ParseParameters(policyExtension.SqlParams, context);
                    foreach (var param in parameters)
                    {
                        queryParameters.Add($"@{param.Key}", param.Value);
                    }
                }

                var parametersHash = GetParametersHash(policyExtension.SqlParams, context);
                var extensionCacheKey = $"policy_ext_{policyName}_{policyExtension.PolicyExtensionId}_{parametersHash}";

                var extensionResult = await _cache.GetOrSetAsync<object>(
                        extensionCacheKey,
                        async _ =>
                        {
                            return policyExtension.ReturnType.ToLowerInvariant() switch
                            {
                                "bool" => await _readRepository
                                    .GetLazyRepository<object>().Value
                                    .GetSingleAsync<bool>(policyExtension.SqlQuery, queryParameters, null, cancellationToken, "text"),
                                "list" => (await _readRepository
                                    .GetLazyRepository<string>().Value
                                    .GetListAsync(policyExtension.SqlQuery, cancellationToken, queryParameters, null, "text")).ToList(),
                                "int" => await _readRepository
                                    .GetLazyRepository<object>().Value
                                    .GetSingleAsync<int>(policyExtension.SqlQuery, queryParameters, null, cancellationToken, "text"),
                                "string" => await _readRepository
                                    .GetLazyRepository<object>().Value
                                    .GetSingleAsync<string>(policyExtension.SqlQuery, queryParameters, null, cancellationToken, "text") ?? string.Empty,
                                "keyvalue" => await GetKeyValueResults(policyExtension.SqlQuery, queryParameters, cancellationToken),
                                _ => new object()
                            };
                        },
                        TimeSpan.FromMinutes(30),
                        new[] { CacheTag.ABAC.ToString(), $"policy_ext_{policyName}" },
                        cancellationToken);

                if (policyExtension.ReturnType.ToLowerInvariant() == "keyvalue" && extensionResult is Dictionary<string, object> keyValueDict)
                {
                    foreach (var kvp in keyValueDict)
                    {
                        resource[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    resource[policyExtension.ResourceKey] = extensionResult;
                }
            }
        }
        private async Task<Dictionary<string, object>> GetKeyValueResults(string sqlQuery, DynamicParameters queryParameters, CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, object>();
            var keyValueResults = (await _readRepository
                .GetLazyRepository<dynamic>().Value
                .GetListAsync(sqlQuery, cancellationToken, queryParameters, null, "text")).ToList();

            foreach (var kvResult in keyValueResults)
            {
                var resultDict = kvResult as IDictionary<string, object>;
                if (resultDict is not null)
                {
                    if (resultDict.Count >= 2)
                    {
                        var entries = resultDict.ToList();
                        var key = entries[0].Value?.ToString();
                        var value = entries[1].Value;

                        if (!string.IsNullOrEmpty(key))
                        {
                            //result[key] = value;
                            var processedValue = ProcessValue(value);
                            result[key] = processedValue;
                        }
                    }
                    else
                    {
                        foreach (var column in resultDict)
                        {
                            var columnName = column.Key;
                            var columnValue = column.Value;

                            if (!string.IsNullOrEmpty(columnName))
                            {
                                //result[columnName] = columnValue ?? string.Empty;
                                var processedValue = ProcessValue(columnValue);
                                result[columnName] = processedValue ?? string.Empty;
                            }
                        }
                    }
                }
            }
            return result;
        }
        private object ProcessValue(object? value)
        {
            if (value is string stringValue && IsJsonString(stringValue))
            {
                try
                {
                    return JsonSerializer.Deserialize<Dictionary<string, object>>(stringValue) ?? new Dictionary<string, object>();
                }
                catch (JsonException)
                {
                    // If deserialization fails, return the original string
                    return stringValue;
                }
            }

            return value ?? string.Empty;
        }

        private bool IsJsonString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            // Trim whitespace
            value = value.Trim();

            // Basic JSON structure checks
            return (value.StartsWith("{") && value.EndsWith("}")) ||
                   (value.StartsWith("[") && value.EndsWith("]"));
        }
        private string GetParametersHash(string sqlParams, IDictionary<string, object> context)
        {
            if (string.IsNullOrWhiteSpace(sqlParams))
                return "no_params";

            var parameters = ParseParameters(sqlParams, context);
            var paramString = string.Join("|", parameters.Select(p => $"{p.Key}={p.Value}"));
            return paramString.GetHashCode().ToString();
        }
        private Dictionary<string, object> ParseParameters(string parametersJson, IDictionary<string, object> context)
        {
            var result = new Dictionary<string, object>();
            if (string.IsNullOrWhiteSpace(parametersJson))
                return result;

            var paramConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(parametersJson);

            foreach (var config in paramConfig ?? new Dictionary<string, object>())
            {
                var value = ResolveParameterValue(config.Value.ToString(), context);
                result[config.Key] = value;
            }

            return result;
        }

        private object? ResolveParameterValue(string? valueExpression, IDictionary<string, object> context)
        {
            if (string.IsNullOrWhiteSpace(valueExpression))
                return null;

            // Support expressions like: user.id, resource.assetRegisterId, environment.currentTime, static:someValue
            if (valueExpression.StartsWith("user."))
            {
                var key = valueExpression[5..];
                return context.TryGetValue("user", out var user) &&
                      user is IDictionary<string, object> userDict &&
                      userDict.TryGetValue(key, out var value) ? value : null;
            }

            if (valueExpression.StartsWith("resource."))
            {
                var key = valueExpression[9..];
                return context.TryGetValue("resource", out var resource) &&
                       resource is IDictionary<string, object> resourceDict &&
                       resourceDict.TryGetValue(key, out var value) ? value?.ToString() : null;
            }

            if (valueExpression.StartsWith("environment."))
            {
                var key = valueExpression[12..];
                return context.TryGetValue("environment", out var env) &&
                       env is IDictionary<string, object> envDict &&
                       envDict.TryGetValue(key, out var value) ? value : null;
            }

            if (valueExpression.StartsWith("static:"))
            {
                return valueExpression[7..];
            }

            // Direct context lookup
            return context.TryGetValue(valueExpression, out var directValue) ? directValue : valueExpression;
        }
        private async Task<UserResult?> GetUserByUserSyncId(string userSyncId, CancellationToken cancellationToken)
        {
            string sql = @"SELECT *
                          FROM [dbo].[User]
                          WHERE [UserSyncId]=@UserSyncId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserSyncId", new Guid(userSyncId), dbType: DbType.Guid);
            //return await _readRepository.GetLazyRepository<UserResult>().Value.GetAsync(sql, cancellationToken, queryParameters, null, "text");
            var cacheKey = $"user_by_sync_id_{userSyncId}";
            return await _cache.GetOrSetAsync<UserResult?>(
                cacheKey,
                async _ => await _readRepository.GetLazyRepository<UserResult>().Value.GetAsync(sql, cancellationToken, queryParameters, null, "text"),
                TimeSpan.FromMinutes(30),
                new[] { CacheTag.ABAC.ToString() },
                cancellationToken
                );
        }
        private async Task<List<string>> GetClubsByUserId(int userId, CancellationToken cancellationToken)
        {
            string sql = @"SELECT d.SyncGuid FROM [dbo].[Document] d
	                            INNER JOIN [dbo].[Hierarchies] h ON h.EntityId=d.DocId
	                            INNER JOIN [dbo].[HierarchyLinks] hl ON hl.HierarchyId=h.Id
	                            INNER JOIN [User] u ON u.Userid = hl.UserId
                            WHERE d.RepositoryId=2
	                            AND u.Userid=@UserId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", userId);
            var cacheKey = $"clubs_in_{userId}";
            var result = await _cache.GetOrSetAsync<List<AdminClubResult>>(
                cacheKey,
                async _ => (await _readRepository.GetLazyRepository<AdminClubResult>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList(),
                TimeSpan.FromMinutes(30),
                new[] { CacheTag.ABAC.ToString() },
                cancellationToken
                );
            return result.Select(x => (string)x.SyncGuid).ToList();
        }
        private async Task<List<string>> GetAdminClubsWithChildByUserIdQuery(int userId, CancellationToken cancellationToken)
        {
            string sql = "[dbo].[GetAdminClubsWithChildByUserId]";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", userId);
            var cacheKey = $"admin_clubs_with_child_{userId}";
            var result = await _cache.GetOrSetAsync<List<AdminClubResult>>(
                cacheKey,
                async _ => (await _readRepository.GetLazyRepository<AdminClubResult>().Value.GetListAsync(sql, cancellationToken, queryParameters)).ToList(),
                TimeSpan.FromMinutes(30),
                new[] { CacheTag.ABAC.ToString() },
                cancellationToken
                );
            return result.Select(x => (string)x.SyncGuid).ToList();
        }
        private async Task<List<string>> GetPermissionsByUserIdQuery(int userId, CancellationToken cancellationToken)
        {
            string sql = @"SELECT DISTINCT ap.[Id],ap.[Permission]
                            FROM [dbo].[AbacPermissions] ap
	                            INNER JOIN [dbo].[AbacRolePermissions] arp ON ap.[Id]=arp.[PermissionId]
	                            INNER JOIN [dbo].[AbacRoles] ar ON ar.Id=arp.[RoleId]
	                            INNER JOIN [dbo].[AbacUserRoles] aur ON aur.[RoleId]=ar.[Id]
                            WHERE aur.[UserId]=@UserId";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserId", userId);
            var cacheKey = $"user_permissions_{userId}";
            var result = await _cache.GetOrSetAsync<List<PermissionResult>>(
                cacheKey,
                async _ => (await _readRepository.GetLazyRepository<PermissionResult>().Value.GetListAsync(sql, cancellationToken, queryParameters, null, "text")).ToList(),
                TimeSpan.FromMinutes(30),
                new[] { CacheTag.ABAC.ToString() },
                cancellationToken
                );
            return result.Select(x => (string)x.Permission).ToList();
        }
        public async Task<IDictionary<string, FieldPermission>> GetFieldPermissions<T>(T obj, string policyPrefix, Dictionary<string, object>? resource, CancellationToken cancellationToken)
        {
            var permissions = new Dictionary<string, FieldPermission>();
            var fields = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var policyName = $"{policyPrefix}_{field.Name}";
                var result = await EvaluatePolicyMultiAsync(policyName, cancellationToken, null, resource);
                if (result is not null)
                {
                    permissions[field.Name] = result.Values?.FirstOrDefault() ?? CreateDefaultPermissions();
                }
            }
            return permissions;
        }

        public async Task<object> EvaluateCombinedPoliciesAsync(string[] policyNames, string[] policyTypes, CancellationToken cancellationToken, string? action = null, Dictionary<string, object>? resource = null)
        {
            var actions = action != null ? Enumerable.Repeat(action, policyNames.Length).ToArray() : null;
            var resources = resource != null ? Enumerable.Repeat(resource, policyNames.Length).ToArray() : null;

            return await EvaluateCombinedPoliciesAsync(policyNames, policyTypes, cancellationToken, actions, resources);
        }
        public async Task<object> EvaluateCombinedPoliciesAsync(string[] policyNames, string[] policyTypes, CancellationToken cancellationToken, string[]? actions = null, Dictionary<string, object>[]? resources = null)
        {
            if (policyNames.Length != policyTypes.Length)
                throw new ArgumentException("Policy names and types arrays must have the same length");

            var tasks = new List<Task<KeyValuePair<string, object>>>();
            var results = new Dictionary<string, object>();
            var policyCount = policyNames.Length;
            for (int i = 0; i < policyCount; i++)
            {
                var policyName = policyNames[i];
                var policyType = policyTypes[i];
                var action = actions?.Length > i ? actions[i] : null;
                var resource = resources?.Length > i ? resources[i] : null;

                if (policyType.Equals("ui", StringComparison.OrdinalIgnoreCase))
                {
                    var task = EvaluatePolicyAsync(policyName, cancellationToken, action, resource)
                        .ContinueWith(t => new KeyValuePair<string, object>(policyType, t.Result), cancellationToken);
                    tasks.Add(task);
                }
                else if (policyType.Equals("fields", StringComparison.OrdinalIgnoreCase))
                {
                    var task = EvaluatePolicyMultiAsync(policyName, cancellationToken, action, resource)
                        .ContinueWith(t => new KeyValuePair<string, object>(policyType, t.Result), cancellationToken);
                    tasks.Add(task);
                }
            }

            var taskResults = await Task.WhenAll(tasks);

            foreach (var result in taskResults)
            {
                results[result.Key] = result.Value;
            }

            return results;
        }


        public List<string> GetModifiedFields<T1, T2>(T1 newModel, T2 existingModel)
        {
            if (newModel == null || existingModel == null)
            {
                throw new ArgumentNullException("Models cannot be null.");
            }

            var modifiedFields = new List<string>();

            var newProperties = typeof(T1).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var existingProperties = typeof(T2).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var newProperty in newProperties)
            {
                var existingProperty = existingProperties.FirstOrDefault(p => p.Name == newProperty.Name);

                if (existingProperty != null && newProperty.CanRead && existingProperty.CanRead)
                {
                    var oldValue = existingProperty.GetValue(existingModel)?.ToString();
                    var newValue = newProperty.GetValue(newModel)?.ToString();

                    if (!AreFieldValuesEqual(oldValue, newValue))
                    {
                        modifiedFields.Add(newProperty.Name);
                    }
                }
            }

            return modifiedFields;

        }

        private bool AreFieldValuesEqual(object? oldValue, object? newValue)
        {
            // Handle null/empty/whitespace string cases
            if (IsNullOrWhiteSpaceString(oldValue) && IsNullOrWhiteSpaceString(newValue))
                return true;

            // Handle numeric cases
            if (IsNumeric(oldValue) && IsNumeric(newValue))
                return Convert.ToDecimal(oldValue) == Convert.ToDecimal(newValue);

            // Default comparison
            return object.Equals(oldValue, newValue);
        }
        private bool IsNullOrWhiteSpaceString(object? value)
        {
            if (value == null)
                return true;

            if (value is string str)
                return string.IsNullOrWhiteSpace(str);

            return false;
        }
        private bool IsNumeric(object? value)
        {
            if (value == null) return false;

            if (value is sbyte || value is byte ||
                   value is short || value is ushort ||
                   value is int || value is uint ||
                   value is long || value is ulong ||
                   value is float || value is double ||
                   value is decimal)
            {
                return true;
            }
            if (value is string str)
            {
                return double.TryParse(str, out _);
            }
            if (value is JsonElement jsonElement)
            {
                return jsonElement.ValueKind == JsonValueKind.Number;
            }
            // Try parsing the string representation as a fallback
            var stringValue = value.ToString();
            return !string.IsNullOrEmpty(stringValue) && double.TryParse(stringValue, out _);
        }
        public List<string> GetModifiedFields(Dictionary<string, object> newModel, Dictionary<string, object> existingModel)
        {
            if (newModel == null || existingModel == null)
            {
                throw new ArgumentNullException("Models cannot be null.");
            }

            var modifiedFields = new List<string>();
            GetModifiedFieldsRecursive(newModel, existingModel, "", modifiedFields);
            return modifiedFields.Distinct().ToList();
        }

        private void GetModifiedFieldsRecursive(Dictionary<string, object> newModel, Dictionary<string, object> existingModel, string prefix, List<string> modifiedFields)
        {
            // Check all keys from the new model
            foreach (var kvp in newModel)
            {
                var key = kvp.Key;
                var newValue = kvp.Value;
                var fieldPath = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";

                if (existingModel.TryGetValue(key, out var existingValue))
                {
                    // Handle nested dictionaries/objects
                    if (IsNestedDictionary(newValue) && IsNestedDictionary(existingValue))
                    {
                        var newDict = ConvertToStringObjectDictionary(newValue);
                        var existingDict = ConvertToStringObjectDictionary(existingValue);

                        if (newDict != null && existingDict != null)
                        {
                            GetModifiedFieldsRecursive(newDict, existingDict, fieldPath, modifiedFields);
                            continue;
                        }
                    }

                    // Compare values for non-nested fields
                    if (!AreValuesEqual(newValue, existingValue))
                    {
                        // For top-level fields, just use the key name
                        modifiedFields.Add(string.IsNullOrEmpty(prefix) ? key : fieldPath);
                    }
                }
                else
                {
                    if (newValue is not null && !string.IsNullOrWhiteSpace(newValue?.ToString()))
                    {
                        // Key exists in new model but not in existing model (new field)
                        modifiedFields.Add(string.IsNullOrEmpty(prefix) ? key : fieldPath);
                    }

                }
            }
        }

        private bool IsNestedDictionary(object value)
        {
            if (value == null) return false;

            // Handle JsonElement objects
            if (value is JsonElement jsonElement)
            {
                return jsonElement.ValueKind == JsonValueKind.Object;
            }

            // Handle regular dictionaries
            return value is Dictionary<string, object> ||
                   value is IDictionary<string, object> ||
                   (value.GetType().IsGenericType &&
                    value.GetType().GetGenericTypeDefinition() == typeof(Dictionary<,>));
        }

        private Dictionary<string, object>? ConvertToStringObjectDictionary(object value)
        {
            if (value == null) return null;

            // Handle JsonElement objects
            if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
            {
                var dict = new Dictionary<string, object>();
                foreach (var property in jsonElement.EnumerateObject())
                {
                    dict[property.Name] = property.Value;
                }
                return dict;
            }

            // Handle existing Dictionary<string, object>
            if (value is Dictionary<string, object> stringObjectDict)
            {
                return stringObjectDict;
            }

            // Handle IDictionary<string, object>
            if (value is IDictionary<string, object> iDict)
            {
                return new Dictionary<string, object>(iDict);
            }

            return null;
        }

        private bool AreValuesEqual(object? value1, object? value2)
        {
            // Handle null/empty/whitespace string cases
            if (IsNullOrWhiteSpaceString(value1) && IsNullOrWhiteSpaceString(value2))
                return true;

            // Handle numeric cases
            if (IsNumeric(value1) && IsNumeric(value2))
                return Convert.ToDecimal(value1) == Convert.ToDecimal(value2);

            // Handle JsonElement comparisons
            if (value1 is JsonElement json1 && value2 is JsonElement json2)
            {
                return JsonElementEquals(json1, json2);
            }

            // Handle JsonElement vs other types
            if (value1 is JsonElement json1Only)
            {
                return JsonElementEqualsObject(json1Only, value2);
            }
            if (value2 is JsonElement json2Only)
            {
                return JsonElementEqualsObject(json2Only, value1);
            }

            // Handle arrays/lists (but not strings)
            if (value1 is IEnumerable<object> enum1 && value2 is IEnumerable<object> enum2 &&
                !(value1 is string) && !(value2 is string))
            {
                return enum1.SequenceEqual(enum2);
            }

            // Default string comparison for primitive types
            return value1.ToString() == value2.ToString();
        }

        private bool JsonElementEquals(JsonElement json1, JsonElement json2)
        {
            if (json1.ValueKind != json2.ValueKind) return false;

            return json1.ValueKind switch
            {
                JsonValueKind.Null => true,
                JsonValueKind.True or JsonValueKind.False => json1.GetBoolean() == json2.GetBoolean(),
                JsonValueKind.Number => json1.GetRawText() == json2.GetRawText(),
                JsonValueKind.String => json1.GetString() == json2.GetString(),
                JsonValueKind.Array => JsonArrayEquals(json1, json2),
                JsonValueKind.Object => JsonObjectEquals(json1, json2),
                _ => json1.GetRawText() == json2.GetRawText()
            };
        }

        private bool JsonElementEqualsObject(JsonElement jsonElement, object obj)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.Null => obj == null,
                JsonValueKind.True => obj is bool b && b,
                JsonValueKind.False => obj is bool b2 && !b2,
                JsonValueKind.Number => jsonElement.GetRawText() == obj?.ToString(),
                JsonValueKind.String => jsonElement.GetString() == obj?.ToString(),
                _ => jsonElement.GetRawText() == obj?.ToString()
            };
        }

        private bool JsonArrayEquals(JsonElement json1, JsonElement json2)
        {
            if (json1.GetArrayLength() != json2.GetArrayLength()) return false;

            var array1 = json1.EnumerateArray().ToArray();
            var array2 = json2.EnumerateArray().ToArray();

            for (int i = 0; i < array1.Length; i++)
            {
                if (!JsonElementEquals(array1[i], array2[i]))
                    return false;
            }

            return true;
        }

        private bool JsonObjectEquals(JsonElement json1, JsonElement json2)
        {
            var props1 = json1.EnumerateObject().ToArray();
            var props2 = json2.EnumerateObject().ToArray();

            if (props1.Length != props2.Length) return false;

            var dict2 = props2.ToDictionary(p => p.Name, p => p.Value);

            foreach (var prop1 in props1)
            {
                if (!dict2.TryGetValue(prop1.Name, out var value2) ||
                    !JsonElementEquals(prop1.Value, value2))
                {
                    return false;
                }
            }

            return true;
        }

        public string GetPolicyPrefix(string extensionArea)
        {
            var prefix = string.Empty;

            if (extensionArea.ToLowerInvariant().Equals("asset"))
            {
                prefix = "asset";
            }
            else if (extensionArea.ToLowerInvariant().Equals("profile"))
            {
                prefix = "member";
            }

            return prefix;
        }


    }
}
#endif