using System.Text.Json;
using Dapper;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Exceptions;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Result.Application.DTOs.ImportExportFileDtos;

namespace JustGo.Result.Application.Features.MemberUpload.Helpers;

internal static class MemberUploadHelper
{
    public static async Task<ResolvedValidationScopedDto> ResolveValidationScopeDependency(
        IReadRepositoryFactory readRepository,
        int scopeReferenceId, CancellationToken cancellationToken)
    {
        var validationScopeFields =
            await GetValidationScopeFieldMappingAsync(readRepository, scopeReferenceId);

        var validationScopeIds =
            GetValidationScopeIdsFromFieldMappings(validationScopeFields, scopeReferenceId);

        var memberIdHeaders =
            await GetValidatedMemberIdHeaders(readRepository, validationScopeIds,
                cancellationToken);
        
        var assetIdHeader =
            await GetValidatedAssetIdHeader(readRepository, validationScopeIds,
                cancellationToken);

        var shouldResolveValidationScope = validationScopeFields.Count > 0;
        var headerName = string.Empty;

        if (shouldResolveValidationScope)
        {
            headerName = TryGetHeaderName(validationScopeFields, scopeReferenceId);
        }

        return new ResolvedValidationScopedDto
        {
            ShouldResolveValidationScope = shouldResolveValidationScope,
            HeaderName = headerName,
            ValidationScopeFieldMappings = validationScopeFields,
            ValidatedMemberIdHeaders = memberIdHeaders,
            ValidationScopeIds = validationScopeIds,
            ValidatedAssetIdHeader = assetIdHeader ?? string.Empty
        };
    }

    private static List<int> GetValidationScopeIdsFromFieldMappings(
        IEnumerable<ValidationScopeFieldMappingDto> fieldMappings, params int[] additionalScopeIds)
    {
        var validationScopeIds = new HashSet<int>();
        foreach (var mapping in fieldMappings)
        {
            validationScopeIds.Add(mapping.TargetValidationScopeId);
        }

        foreach (var id in additionalScopeIds)
        {
            validationScopeIds.Add(id);
        }

        return validationScopeIds.ToList();
    }

    private static async Task<IList<ValidationScopeFieldMappingDto>> GetValidationScopeFieldMappingAsync(
        IReadRepositoryFactory readRepository,
        int validationScopeId)
    {
        const string sql = """
                           SELECT ValidationScopeId, HeaderName, HeaderValue, TargetValidationScopeId, LogicalOperator
                           FROM ValidationScopeFieldMapping 
                           WHERE ValidationScopeId = @ValidationScopeId
                           """;

        return (await readRepository.GetRepository<ValidationScopeFieldMappingDto>().GetListAsync(
            sql, new { ValidationScopeId = validationScopeId }, null, commandType: QueryType.Text)).ToList();
    }

    private static async Task<IList<(int ValidationScopeId, string ValidationItemDisplayName)>>
        GetValidatedMemberIdHeaders(
            IReadRepositoryFactory readRepository,
            IEnumerable<int> validationScopeIds,
            CancellationToken cancellationToken)
    {
        const string sql = """
                           select vsc.ValidationScopeId, vs.ValidationItemDisplayName from ValidationSchema vs
                           inner join  ValidationSchemaScope vss on vs.ValidationSchemaId = vss.ValidationSchemaId
                           inner join ValidationScopes vsc on vss.ValidationScopeId = vsc.ValidationScopeId
                           where vsc.ValidationScopeId in @ValidationScopeIds and ValidationItemName = 'MemberId'
                           """;
        var queryParameters = new DynamicParameters();
        queryParameters.Add("@ValidationScopeIds", validationScopeIds);
        var repo = readRepository.GetRepository<object>();
        var result =
            await repo.GetListAsync<(int ValidationScopeId, string ValidationItemDisplayName)>(sql, queryParameters,
                null, QueryType.Text,
                cancellationToken);
        return result.ToList();
    }

    private static async Task<string?>
        GetValidatedAssetIdHeader(
            IReadRepositoryFactory readRepository,
            IEnumerable<int> validationScopeIds,
            CancellationToken cancellationToken)
    {
        const string sql = """
                           select vs.ValidationItemDisplayName from ValidationSchema vs
                           inner join  ValidationSchemaScope vss on vs.ValidationSchemaId = vss.ValidationSchemaId
                           inner join ValidationScopes vsc on vss.ValidationScopeId = vsc.ValidationScopeId
                           where vsc.ValidationScopeId in @ValidationScopeIds and ValidationItemName = 'AssetId'
                           """;
        var queryParameters = new DynamicParameters();
        queryParameters.Add("@ValidationScopeIds", validationScopeIds);
        var repo = readRepository.GetRepository<object>();
        var result =
            await repo.QueryFirstAsync<string>(sql, queryParameters,
                null, QueryType.Text,
                cancellationToken);
        return result;
    }

    internal static void ResolveValidationScopeId(Dictionary<string, string> row,
        IList<ValidationScopeFieldMappingDto> validationScopeField, string? headerName, ref int targetValidationScopeId)
    {
        // if (!TryGetValueIgnoreCase(row!, headerName, out var actualValue) ||
        //     string.IsNullOrEmpty(actualValue?.ToString()))
        // {
        //     return;
        //     // throw new CustomValidationException(
        //     //     $"Header '{headerName}' is missing or has no value at row index {index+1}.");
        // }

        TryGetValueIgnoreCase(row!, headerName, out var actualValue);

        var actualValueString = actualValue?.ToString() ?? string.Empty;

        foreach (var validationScopeFieldMappingDto in validationScopeField)
        {
            if (!Enum.IsDefined(typeof(LogicalOperator), validationScopeFieldMappingDto.LogicalOperator))
            {
                throw new CustomValidationException(
                    $"Invalid logical operator value: {validationScopeFieldMappingDto.LogicalOperator}");
            }

            var logicalOperator = (LogicalOperator)validationScopeFieldMappingDto.LogicalOperator;
            
            var isMatch = logicalOperator switch
            {
                LogicalOperator.And => validationScopeFieldMappingDto.ParsedHeaderValues
                    .TrueForAll(parsedHeaderValue => parsedHeaderValue.Evaluate(actualValueString)),

                LogicalOperator.Or => validationScopeFieldMappingDto.ParsedHeaderValues
                    .Any(parsedHeaderValue => parsedHeaderValue.Evaluate(actualValueString)),

                _ => throw new CustomValidationException(
                    $"Unsupported logical operator: {logicalOperator} (value: {validationScopeFieldMappingDto.LogicalOperator})")
            };

            if (!isMatch) continue;
            targetValidationScopeId = validationScopeFieldMappingDto.TargetValidationScopeId;
            break;
        }
    }

    private static string TryGetHeaderName(IList<ValidationScopeFieldMappingDto> validationScopeFields,
        int validationScopeId)
    {
        var headerName = validationScopeFields.FirstOrDefault()?.HeaderName;

        if (string.IsNullOrWhiteSpace(headerName))
        {
            throw new CustomValidationException(
                $"Internal configuration error: HeaderName for ValidationScopeId {validationScopeId} is not defined properly.");
        }

        return headerName;
    }

    private static bool TryGetValueIgnoreCase(Dictionary<string, string?> dictionary, string? key,
        out object? actualValue)
    {
        actualValue = null;

        if (string.IsNullOrEmpty(key) || dictionary.Count == 0)
            return false;

        var normalizedSearchKey = key.Replace(" ", "");

        foreach (var kvp in dictionary)
        {
            if (string.IsNullOrEmpty(kvp.Key))
                continue;

            var keyWithoutSpaces = kvp.Key.Replace(" ", "");
            if (keyWithoutSpaces.Length != normalizedSearchKey.Length)
                continue;

            if (!string.Equals(keyWithoutSpaces, normalizedSearchKey, StringComparison.OrdinalIgnoreCase)) continue;
            actualValue = kvp.Value;
            return true;
        }

        return false;
    }

    internal static Dictionary<string, string?> PopulateDynamicProperties(string memberData)
    {
        var data = new Dictionary<string, string?>();

        if (string.IsNullOrWhiteSpace(memberData))
        {
            return new Dictionary<string, string?>();
        }

        try
        {
            using var document = JsonDocument.Parse(memberData);
            var root = document.RootElement;

            foreach (var property in root.EnumerateObject())
            {
                var value = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => property.Value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => string.Empty,
                    _ => property.Value.GetRawText()
                };

                data[property.Name] = value;
            }

            return data;
        }
        catch (JsonException)
        {
            data.Clear();
            throw new CustomValidationException("Invalid JSON format in MemberData.");
        }
    }
}

public enum LogicalOperator : int
{
    Or = 0,
    And = 1
}