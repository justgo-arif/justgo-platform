using System.Text.Json;
using Newtonsoft.Json;
using JsonException = System.Text.Json.JsonException;

namespace JustGo.Result.Application.DTOs.UploadResultDtos;

public class ResultMemberDataDto
{
    public int Id { get; set; }

    public required string FileName { get; set; }

    public required string MemberId { get; set; }

    public required string MemberName { get; set; }

    [JsonIgnore] public int TotalCount { get; set; }
    public int ErrorCount { get; set; }

    public string? ValidationStatus { get; set; }
    public string? ErrorMessage { get; set; }

    [JsonIgnore] public bool ShouldIncludeErrors { get; set; } = true;

    public bool ShouldSerializeErrorType()
    {
        return !string.IsNullOrEmpty(ValidationStatus) && ShouldIncludeErrors;
    }

    public bool ShouldSerializeErrorMessage()
    {
        return !string.IsNullOrEmpty(ValidationStatus) && ShouldIncludeErrors;
    }

    [System.Text.Json.Serialization.JsonIgnore] private string MemberData { get; set; } = string.Empty;

    public required Dictionary<string, string> DynamicProperties { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public void PopulateDynamicProperties(params string[] propertyToIgnore)
    {
        DynamicProperties.Clear();

        if (string.IsNullOrWhiteSpace(MemberData))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(MemberData);
            var root = document.RootElement;

            foreach (var property in root.EnumerateObject())
            {
                if (propertyToIgnore.Contains(property.Name))
                    continue;

                var value = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => property.Value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => string.Empty,
                    _ => property.Value.GetRawText()
                };

                DynamicProperties[property.Name] = value;
            }
        }
        catch (JsonException)
        {
            DynamicProperties.Clear();
        }
    }
}