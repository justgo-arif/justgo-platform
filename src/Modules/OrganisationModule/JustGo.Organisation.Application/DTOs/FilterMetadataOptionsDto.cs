namespace JustGo.Organisation.Application.DTOs
{
    public class FilterMetadataOptionsDto
    {
        public List<SelectListItemDTO<string>> Regions { get; set; }
        public List<SelectListItemDTO<string>> ClubTypes { get; set; }
    }
}
