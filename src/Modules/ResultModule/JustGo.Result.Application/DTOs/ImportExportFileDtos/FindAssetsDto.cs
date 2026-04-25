namespace JustGo.Result.Application.DTOs.ImportExportFileDtos
{
    public class FindAssetsDto
    {
        public string HorseId { get; set; } = string.Empty;
        public string HorseName { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string PrimaryLicense { get; set; } = string.Empty;
        public string AdditionalLicense { get; set; } = string.Empty;
    }
}