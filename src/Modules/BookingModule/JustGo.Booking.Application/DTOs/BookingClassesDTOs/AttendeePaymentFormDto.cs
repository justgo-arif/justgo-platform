namespace JustGo.Booking.Application.DTOs.BookingClassesDTOs;

public class AttendeePaymentFormDto
{
    public int FieldId { get; set; }
    public required string Class { get; set; }
    public int DataType { get; set; }
    public string? Value { get; set; }
    public required string DisplayType { get; set; }
    public string? DisplayValue { get; set; }
    public required string Question { get; set; }
    public required bool IsRequired { get; set; }
}
