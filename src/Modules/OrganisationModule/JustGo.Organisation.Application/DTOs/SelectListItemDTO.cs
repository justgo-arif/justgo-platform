namespace JustGo.Organisation.Application.DTOs
{
    public class SelectListItemDTO<T>
    {
        public required T Id { get; set; }
        public required string Text { get; set; }
        public string? Value { get; set; }
    }
}
