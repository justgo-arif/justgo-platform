using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using JustGo.Authentication.Helper.Enums;
using JustGo.Authentication.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.Result.Application.Features.ResultView.Queries.GetResults;

public class GetResultViewQuery : IRequest<Result<object>>, IValidatableObject
{
    public int CompetitionId { get; set; }
    public int? RoundId { get; set; }
    
    [Required] public int PageNumber { get; set; } = 1;
    [Required] public int PageSize { get; set; } = 10;
    
    public string? Search { get; set; } = string.Empty;
    
    public string? SortBy { get; set; }
    
    public string? OrderBy { get; set; }
    
    public string? FilterJson { get; set; }
    
    [JsonIgnore]
    public SportType SportType { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PageNumber <= 0)
        {
            yield return new ValidationResult("PageNumber must be greater than 0.",
                [nameof(PageNumber)]);
        }

        if (PageSize <= 0)
        {
            yield return new ValidationResult("PageSize must be greater than 0.",
                [nameof(PageSize)]);
        }

        var validOrderByValues = new[] { "ASC", "DESC" };
        if (!string.IsNullOrEmpty(OrderBy) && !validOrderByValues.Contains(OrderBy.ToUpper()))
        {
            yield return new ValidationResult("OrderBy must be either 'ASC' or 'DESC'.",
                [nameof(OrderBy)]);
        }

        if (string.IsNullOrEmpty(FilterJson)) yield break;
        
        if (!IsValidJson(FilterJson))
        {
            yield return new ValidationResult("FilterJson must be a valid JSON Array.",
                [nameof(FilterJson)]);
        }
    }
    
    private static bool IsValidJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
    
            // Expecting a JSON array at the root: \[ { "key": "...", "values": \[ ... \] }, ... \]
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }
    
            foreach (var element in document.RootElement.EnumerateArray())
            {
                // Each item must be an object
                if (element.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }
    
                // \`key\` should be a string
                if (!element.TryGetProperty("key", out var keyProp) ||
                    keyProp.ValueKind != JsonValueKind.String)
                {
                    return false;
                }
    
                // \`values\` should be an array (even if it has a single element)
                if (!element.TryGetProperty("values", out var valuesProp) ||
                    valuesProp.ValueKind != JsonValueKind.Array)
                {
                    return false;
                }
            }
    
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}