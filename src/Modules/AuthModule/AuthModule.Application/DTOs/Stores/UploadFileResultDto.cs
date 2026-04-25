namespace AuthModule.Application.DTOs.Stores
{
    public class UploadFileResultDto
    {
        public bool Success { get; set; }
        public string DownloadUrl { get; set; }
        public string ErrorMessage { get; set; }
        public string Link { get; set; } // For Froala
        public int? ErrorCode { get; set; }
    }
}
