namespace JustGo.Booking.Application.DTOs.BookingCatalogDTOs;

public sealed record DescriptionDto
{
    public required string Name { get; init; }
    public string? Description { get; set; }
}                                          