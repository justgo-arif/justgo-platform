using System.Text.Json.Serialization;

namespace AuthModule.Application.DTOs.Stores;

public class DownloadFileStreamDto
{
    [JsonIgnore]
    public Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
}
