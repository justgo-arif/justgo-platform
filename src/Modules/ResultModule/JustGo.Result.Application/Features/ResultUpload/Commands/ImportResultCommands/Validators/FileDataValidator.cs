using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ImportResultCommands.
    Validators;

public class FileDataValidator : IFileDataValidator
{
    private readonly JsonDocument _config;
    private readonly Dictionary<string, HashSet<string>> _allowedValuesCache;

    private static readonly JsonDocumentOptions Options = new()
    {
        AllowTrailingCommas = true
    };

    public FileDataValidator(string configJson)
    {
        _config = JsonDocument.Parse(configJson, Options);
        _allowedValuesCache = BuildAllowedValuesCache();
    }

    private Dictionary<string, HashSet<string>> BuildAllowedValuesCache()
    {
        var cache = new Dictionary<string, HashSet<string>>();
        var validationRules = _config.RootElement.GetProperty("validationRules");
        var columns = validationRules.GetProperty("columns");

        foreach (var column in columns.EnumerateArray())
        {
            var columnName = column.GetProperty("columnName").GetString();
            if (columnName == null) continue;

            var validation = column.GetProperty("validation");
            var validationType = validation.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;

            if (validationType != "fixed_value" ||
                !validation.TryGetProperty("allowedValues", out var allowedValuesProp)) continue;
            
            var allowedValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var valueElement in allowedValuesProp.EnumerateArray())
            {
                var stringValue = valueElement.GetString();
                allowedValues.Add(stringValue ?? string.Empty);
            }

            cache[columnName] = allowedValues;
        }

        return cache;
    }

    public ICollection<string> ValidateRow(Dictionary<string, string> rowData)
    {
        var errors = new List<string>();
        var validationRules = _config.RootElement.GetProperty("validationRules");
        var columns = validationRules.GetProperty("columns");
        var validatedDates = new Dictionary<string, DateTime>();

        foreach (var column in columns.EnumerateArray())
        {
            var columnName = column.GetProperty("columnName").GetString() ??
                             throw new InvalidOperationException("Column name is missing in validation config");
            var required = column.TryGetProperty("required", out var reqProp) && reqProp.GetBoolean();
            var validation = column.GetProperty("validation");

            var value = rowData.TryGetValue(columnName, out var value1) ? value1 : null;
            var valueStr = value?.Trim() ?? "";

            switch (required)
            {
                case true when string.IsNullOrWhiteSpace(valueStr):
                    errors.Add($"The required field '{columnName}' is missing or empty");
                    continue;

                case false when string.IsNullOrWhiteSpace(valueStr):
                    continue;
            }

            var validationType = validation.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;

            switch (validationType)
            {
                case "date_format":
                    var validatedDate = ValidateDateFormat(valueStr, validation, errors);
                    if (validatedDate.HasValue)
                    {
                        validatedDates[columnName] = validatedDate.Value;
                    }
                    break;
                case "integer":
                    ValidateInteger(columnName, valueStr, validation, errors);
                    break;
                case "decimal":
                    ValidateDecimal(columnName, valueStr, validation, errors);
                    break;
                case "string":
                    ValidateString(valueStr, validation, errors);
                    break;
                case "fixed_value":
                    ValidateFixedValue(columnName, valueStr, validation, errors, _allowedValuesCache);
                    break;
            }
        }
    
        ValidateCrossColumnRules(validatedDates, errors);
        return errors;
    }
    
    private void ValidateCrossColumnRules(Dictionary<string, DateTime> validatedDates, List<string> errors)
    {
        var validationRules = _config.RootElement.GetProperty("validationRules");
        
        if (! validationRules.TryGetProperty("crossColumnValidations", out var crossValidations))
            return;

        foreach (var rule in crossValidations.EnumerateArray())
        {
            var validationType = rule.TryGetProperty("validationType", out var typeProp) 
                ? typeProp.GetString() 
                : null;

            if (validationType == "date_range_check")
            {
                ValidateDateRange(rule, validatedDates, errors);
            }
        }
    }
    
    private static void ValidateDateRange(JsonElement rule, Dictionary<string, DateTime> validatedDates, List<string> errors)
    {
        var column1Info = rule.GetProperty("column1");
        var column2Info = rule.GetProperty("column2");
        
        var column1Name = column1Info.GetProperty("columnName").GetString();
        var column2Name = column2Info.GetProperty("columnName").GetString();
        
        if (column1Name == null || column2Name == null)
            return;
        
        if (!validatedDates.TryGetValue(column1Name, out var date1) ||
            !validatedDates. TryGetValue(column2Name, out var date2))
        {
            return;
        }

        var comparison = rule.TryGetProperty("comparison", out var compProp) 
            ? compProp.GetString() 
            : "less_than_or_equal";
        
        var errorMessage = rule.TryGetProperty("errorMessage", out var msgProp)
            ? msgProp.GetString()
            : $"{column1Name} must be less than or equal to {column2Name}";

        bool isValid = comparison switch
        {
            "less_than" => date1 < date2,
            "less_than_or_equal" => date1 <= date2,
            "greater_than" => date1 > date2,
            "greater_than_or_equal" => date1 >= date2,
            "equal" => date1 == date2,
            "not_equal" => date1 != date2,
            _ => date1 <= date2 // default to less_than_or_equal
        };

        if (!isValid)
        {
            errors.Add(errorMessage! );
        }
    }
    
    // private static void ValidateDateFormat(string value, JsonElement validation,
    //     List<string> errors)
    // {
    //     var pattern = validation.TryGetProperty("pattern", out var patternProp)
    //         ? patternProp.GetString()
    //         : @"^(0[1-9]|[12][0-9]|3[01])\/(0[1-9]|1[0-2])\/(\d{4})$";
    //
    //     var errorMessage = validation.TryGetProperty("errorMessage", out var msgProp)
    //         ? msgProp.GetString()
    //         : "Invalid date format. Expected DD/MM/YYYY";
    //     
    //     var datePart = value.Trim().Split(' ')[0];
    //
    //     if (!Regex.IsMatch(datePart, pattern!))
    //     {
    //         errors.Add(errorMessage!);
    //         return;
    //     }
    //     
    //     string[] acceptedFormats = { "dd/MM/yyyy", "d/M/yyyy", "dd/M/yyyy", "d/MM/yyyy" };
    //
    //     if (!DateTime.TryParseExact(datePart, acceptedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
    //     {
    //         errors.Add($"Invalid date value: {value}");
    //     }
    // }
    
    private static DateTime? ValidateDateFormat(string value, JsonElement validation, List<string> errors)
    {
        var pattern = validation. TryGetProperty("pattern", out var patternProp)
            ? patternProp.GetString()
            : @"^(0[1-9]|[12][0-9]|3[01])\/(0[1-9]|1[0-2])\/(\d{4})$";

        var errorMessage = validation.TryGetProperty("errorMessage", out var msgProp)
            ? msgProp.GetString()
            : "Invalid date format. Expected DD/MM/YYYY";
        
        var datePart = value.Trim().Split(' ')[0];

        if (!Regex.IsMatch(datePart, pattern!))
        {
            errors.Add(errorMessage!);
            return null;
        }
        
        string[] acceptedFormats = { "dd/MM/yyyy", "d/M/yyyy", "dd/M/yyyy", "d/MM/yyyy" };

        if (DateTime.TryParseExact(datePart, acceptedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            return parsedDate;
        }
        
        errors.Add($"Invalid date value: {value}");
        return null;
    }

    private static void ValidateInteger(string column, string value, JsonElement validation,
        List<string> errors)
    {
        var errorMessage = validation.TryGetProperty("errorMessage", out var msgProp)
            ? msgProp.GetString()
            : "Value must be a valid integer";

        if (!int.TryParse(value, out int intValue))
        {
            errors.Add(errorMessage!);
            return;
        }

        if (validation.TryGetProperty("min", out var minProp))
        {
            var min = minProp.GetInt32();
            if (intValue < min)
            {
                errors.Add($"{column} must be >= {min}");
                return;
            }
        }

        if (validation.TryGetProperty("max", out var maxProp))
        {
            var max = maxProp.GetInt32();
            if (intValue > max)
            {
                errors.Add(errorMessage!);
                return;
            }
        }

        if (validation.TryGetProperty("maxDigits", out var maxDigitsProp))
        {
            var maxDigits = maxDigitsProp.GetInt32();
            if (Math.Abs(intValue).ToString().Length > maxDigits)
            {
                errors.Add(errorMessage!);
                return;
            }
        }
    }

    private static void ValidateDecimal(string column, string value, JsonElement validation,
        List<string> errors)
    {
        var errorMessage = validation.TryGetProperty("errorMessage", out var msgProp)
            ? msgProp.GetString()
            : "Value must be a valid decimal number";

        if (!decimal.TryParse(value, out decimal decimalValue))
        {
            errors.Add(errorMessage!);
            return;
        }

        if (validation.TryGetProperty("min", out var minProp))
        {
            var min = minProp.GetDecimal();
            if (decimalValue < min)
            {
                errors.Add(errorMessage!);
                return;
            }
        }

        if (validation.TryGetProperty("max", out var maxProp))
        {
            var max = maxProp.GetDecimal();
            if (decimalValue > max)
            {
                errors.Add(errorMessage!);
                return;
            }
        }

        if (validation.TryGetProperty("decimalPlaces", out var decimalPlacesProp))
        {
            var decimalPlaces = decimalPlacesProp.GetInt32();
            var valueStr = value.TrimEnd('0').TrimEnd('.');
            if (!valueStr.Contains('.')) return;
            var actualDecimals = valueStr.Split('.')[1].Length;
            if (actualDecimals > decimalPlaces)
            {
                errors.Add(errorMessage!);
            }
        }
    }

    private static void ValidateString(string value, JsonElement validation,
        List<string> errors)
    {
        var errorMessage = validation.TryGetProperty("errorMessage", out var msgProp)
            ? msgProp.GetString()
            : "String exceeds maximum length";

        if (validation.TryGetProperty("maxLength", out var maxLengthProp))
        {
            var maxLength = maxLengthProp.GetInt32();
            if (value.Length > maxLength)
            {
                errors.Add(errorMessage!);
            }
        }
    }

    private static void ValidateFixedValue(string column, string value, JsonElement validation,
        List<string> errors, Dictionary<string, HashSet<string>> allowedValuesCache)
    {
        if (!allowedValuesCache.TryGetValue(column, out var allowedValues))
            return;

        var errorMessage = validation.TryGetProperty("errorMessage", out var msgProp)
            ? msgProp.GetString()
            : $"Value must be one of: {string.Join(", ", allowedValues.Select(v => $"'{v}'"))}...";

        if (!allowedValues.Contains(value))
        {
            errors.Add(errorMessage!);
        }
    }
}