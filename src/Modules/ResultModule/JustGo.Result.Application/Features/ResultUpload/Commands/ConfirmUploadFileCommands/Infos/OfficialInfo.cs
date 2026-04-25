using System.Text.Json.Serialization;

namespace JustGo.Result.Application.Features.ResultUpload.Commands.ConfirmUploadFileCommands.Infos;

public class OfficialInfo
{
    [JsonPropertyName("OfficialID")]
    public string OfficialId { get; set; } = string.Empty;
    
    
    [JsonPropertyName("OfficialRoleID")]
    public string OfficialRoleId { get; set; } = string.Empty;
    
    
    [JsonPropertyName("RoleID")]
    public string? RoleId { get; set; }
    
    [JsonPropertyName("ClassName")]
    public string? ClassName { get; set; }
    
    [JsonPropertyName("ClassDate")]
    public string? ClassDate { get; set; }
    
    [JsonPropertyName("TestName")]
    public string? TestName { get; set; }
    
    [JsonPropertyName("CompDate")]
    public string? CompDate { get; set; }
    
    [JsonPropertyName("GradeID")]
    public string? GradeId { get; set; }
    
    [JsonPropertyName("Position")]
    public string? Position { get; set; }
    
    
    [JsonIgnore]
    public string ActualRoleId => !string.IsNullOrWhiteSpace(OfficialRoleId) ? OfficialRoleId : RoleId ?? string.Empty;
    
    public int GetRoleIdAsInt()
    {
        var roleIdValue = ActualRoleId;
        if (!int.TryParse(roleIdValue, out var roleId) || roleId <= 0)
        {
            throw new InvalidOperationException($"Invalid RoleID: '{roleIdValue}'. Expected a positive integer.");
        }
        return roleId;
    }
}