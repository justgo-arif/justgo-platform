namespace JustGo.Result.Application.DTOs.ImportExportFileDtos
{
    public class DynamicMemberDataDto
    {
        public string FileName { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
    }


}
