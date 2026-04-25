namespace JustGo.Result.Application.DTOs.ImportExportFileDtos
{
    public class ImportMemberFileResponseDto
    {
        public int FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
