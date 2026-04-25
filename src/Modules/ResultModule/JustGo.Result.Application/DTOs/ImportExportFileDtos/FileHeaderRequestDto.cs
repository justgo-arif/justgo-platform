using Microsoft.AspNetCore.Http;

namespace JustGo.Result.Application.DTOs.ImportExportFileDtos
{
    public class FileHeaderRequestDto
    {
        public required IFormFile File { get; set; }
        public int DisciplineId { get; set; }
        public string? OwnerGuid { get; set; }
    }
}
