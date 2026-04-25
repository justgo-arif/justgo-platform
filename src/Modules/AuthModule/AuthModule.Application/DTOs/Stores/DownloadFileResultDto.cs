namespace AuthModule.Application.DTOs.Stores
{
    public class DownloadFileResultDto
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public byte[] FileBytes { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }
}
