using Microsoft.AspNetCore.Http;

namespace AuthModule.Application.DTOs.Stores
{
    public class UploadFileRequest
    {
        public IFormFile File { get; set; }
        public string T { get; set; }
        public string P { get; set; }
        public string P1 { get; set; }
        public string P2 { get; set; }
        public string P3 { get; set; }
    }
}
