namespace JustGo.Booking.Application.DTOs.BookingCatalogDTOs;

public sealed record AgeGroupCategoryDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required int MinAge { get; set; }
    public required int MaxAge { get; set; }
    public string? ImageUrl { get; set; }
}