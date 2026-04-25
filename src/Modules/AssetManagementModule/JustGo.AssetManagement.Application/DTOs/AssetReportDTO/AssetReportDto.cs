namespace JustGo.AssetManagement.Application.DTOs.AssetReportDTO
{
    public class AssetReportDto
    {
        public string ReportId { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
        public int ReportType { get; set; } // 1-Core, 2-Custom etc.
        public string ReportPath { get; set; } = string.Empty;
    }
}