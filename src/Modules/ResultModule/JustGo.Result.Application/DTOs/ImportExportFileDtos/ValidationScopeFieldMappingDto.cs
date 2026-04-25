using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using JustGo.Authentication.Infrastructure.Exceptions;

namespace JustGo.Result.Application.DTOs.ImportExportFileDtos;

public class ValidationScopeFieldMappingDto
{
    public int ValidationScopeId { get; set; }

    public string? HeaderName { get; set; } = string.Empty;
    public string? HeaderValue { get; set; } = string.Empty;

    public int TargetValidationScopeId { get; set; }
    
    public int LogicalOperator { get; set; }

    [JsonIgnore]
    public List<ValidationRule> ParsedHeaderValues
    {
        get
        {
            if (string.IsNullOrWhiteSpace(HeaderValue))
                return [];
            
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                if (HeaderValue.Trim().StartsWith('['))
                {
                    return JsonSerializer.Deserialize<List<ValidationRule>>(HeaderValue, options) ?? [];
                }

                var singleRule = JsonSerializer.Deserialize<ValidationRule>(HeaderValue, options);
                return singleRule != null ? [singleRule] : [];
            }
            catch (JsonException)
            {
                throw new CustomValidationException("Invalid JSON format in HeaderValue.");
            }
        }
    }
}

public partial class ValidationRule
{
    [JsonPropertyName("value")] public string Value { get; set; } = string.Empty;

    [JsonPropertyName("operator")] public string Operator { get; set; } = string.Empty;
    
    public bool Evaluate(string input)
    {
        try
        {
            return Operator.ToLowerInvariant() switch
            {
                "contains" => Value.Equals(input, StringComparison.OrdinalIgnoreCase),
                "notcontains" => !Value.Equals(input, StringComparison.OrdinalIgnoreCase),
                "equals" => string.Equals(input.Trim(), Value.Trim(), StringComparison.OrdinalIgnoreCase),
                "notequals" => !string.Equals(input.Trim(), Value.Trim(), StringComparison.OrdinalIgnoreCase),
                "greaterthan" => CompareAsNumbers(input, Value) > 0,
                "lessthan" => CompareAsNumbers(input, Value) < 0,
                "greaterthanorequal" => CompareAsNumbers(input, Value) >= 0,
                "lessthanorequal" => CompareAsNumbers(input, Value) <= 0,
                _ => false
            };
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    private static int CompareAsNumbers(string input, string value)
    {
        var inputResult = ExtractNumericValueWithUnit(input);
        var valueResult = ExtractNumericValueWithUnit(value);
        
        if (!inputResult.IsValid || !valueResult.IsValid)
        {
           // return string.Compare(input, value, StringComparison.OrdinalIgnoreCase);
           throw new CustomValidationException("Invalid numeric value for comparison.");
        }
        
        var inputInMeters = ConvertToMeters(inputResult.NumericValue, inputResult.Unit);
        var valueInMeters = ConvertToMeters(valueResult.NumericValue, valueResult.Unit);
        
        return inputInMeters.CompareTo(valueInMeters);
    }
    
    private static NumericExtractionResult ExtractNumericValueWithUnit(string measurementString)
    {
        if (string.IsNullOrWhiteSpace(measurementString))
        {
            return NumericExtractionResult.Invalid();
        }

        var match = MeasurementRegex().Match(measurementString.Trim());

        if (!match.Success)
        {
            return NumericExtractionResult.Invalid();
        }
        
        var numericPart = match.Groups["number"].Value;
        if (!decimal.TryParse(numericPart, out var numericValue))
        {
            return NumericExtractionResult.Invalid();
        }
        
        var unit = match.Groups["unit"].Success ? match.Groups["unit"].Value.ToLowerInvariant() : "m";

        return new NumericExtractionResult(numericValue, unit, true);
    }
    
    private static decimal ConvertToMeters(decimal value, string unit)
    {
        return unit.ToLowerInvariant() switch
        {
            "cm" => value / 100m,
            "m" => value,
            "" => value, // Assume meters if no unit
            _ => throw new ArgumentException($"Unsupported unit: {unit}", nameof(unit))
        };
    }
    
    [GeneratedRegex(@"^(?<number>\d{1,3}(?:,\d{3})*(?:\.\d+)?|\d+(?:\.\d+)?|\.\d+)(?<unit>cm|m)?$", RegexOptions.IgnoreCase)]
    private static partial Regex MeasurementRegex();


    private readonly record struct NumericExtractionResult(decimal NumericValue, string Unit, bool IsValid)
    {
        public static NumericExtractionResult Invalid() => new(0, string.Empty, false);
    }
}