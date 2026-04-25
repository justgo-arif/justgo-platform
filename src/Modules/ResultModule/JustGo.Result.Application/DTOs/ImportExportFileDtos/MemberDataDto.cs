using Newtonsoft.Json;

namespace JustGo.Result.Application.DTOs.ImportExportFileDtos
{
    public class MemberDataDto
    {
        [JsonIgnore]
        public int TotalCount { get; set; }
        public int Id { get; set; }
        public int FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string MemberData { get; set; } = string.Empty;
        public bool IsValidated { get; set; }
        public string? ErrorType { get; set; }
        public string? ErrorMessage { get; set; }
    }
   
}
