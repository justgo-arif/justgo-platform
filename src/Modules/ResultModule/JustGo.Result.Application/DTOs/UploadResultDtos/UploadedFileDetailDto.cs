using System.Text.Json;

namespace JustGo.Result.Application.DTOs.UploadResultDtos;

public class UploadedFileDetailDto
{
    public bool Marker { get; set; } = false;
    public int? EventId { get; set; }
    
    public int? DisciplineId { get; set; }
    
    public int UploadedFileId { get; set; }
    
    public string FileName { get; set; } = string.Empty;
    
    public string MemberId { get; set; } = string.Empty;
    
    public int UserId { get; set; }
    public string MemberData { get; set; } = string.Empty;
    public Dictionary<string, string?> DynamicProperties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    
    public void PopulateDynamicProperties()
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
