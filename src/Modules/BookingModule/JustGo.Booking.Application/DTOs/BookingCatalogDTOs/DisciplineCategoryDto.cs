namespace JustGo.Booking.Application.DTOs.BookingCatalogDTOs;

public sealed record DisciplineCategoryDto
{
    public required string CategoryGuid { get; set; }
    public required string Name { get; set; }
    public string? ImageUrl { get; set; }
}