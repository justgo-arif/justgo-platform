namespace JustGo.AssetManagement.Application.Features.AssetReports.Commands.DownloadReport
{
    public class DownloadAssetReportResponse
    {
       //public byte[] FileContent { get; set; } = Array.Empty<byte>();
        public string Message { get; set; } = "Connect request to signalR send successfully!";
        public bool IsDownloaded { get; set; } 
    }
}