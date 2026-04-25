namespace AuthModule.Application.DTOs.Stores
{
    public class DownloadTempFileResultDto
    {
        public byte[] FileBytes { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}
