namespace JustGo.Booking.Application.DTOs.BookingCatalogDTOs;

public sealed record BasicClubDetailDto
{
    public required string EntityName { get; set; }
    public string? HeroImageUrl { get; set; }
    public string? LogoUrl { get; set; }
    public bool? HideWaitlist { get; set; }
    public string? ClassBrandColor { get; set; }
    public string? SocialLinks { get; set; }
    public string? Currency { get; set; }
    public string? CurrencySymbol { get; set; }
}