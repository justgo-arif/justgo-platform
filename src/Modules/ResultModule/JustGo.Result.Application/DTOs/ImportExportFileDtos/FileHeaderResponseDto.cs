using JustGo.Result.Domain.Entities;

namespace JustGo.Result.Application.DTOs.ImportExportFileDtos
{
    public class FileHeaderResponseDto
    {
        public int FileId { get; set; }
        public List<string> FileHeaders { get; set; } = [];
        public ICollection<string> SecondSheetHeaders { get; set; } = [];
        public List<ResultUploadFieldMapping> PredefinedHeaders { get; set; } = [];
        public ICollection<ResultUploadFieldMapping> SecondSheetPredefinedHeaders { get; set; } = [];
    }
}
