namespace JustGo.Booking.Application.DTOs.BookingClassesDTOs;

public class GroupedBookingAttendeePaymentDto
{
    public string? PaymentReceiptId { get; set; }
    public int PaymentReceiptDocId { get; set; }
    public decimal TotalAmount { get; set; }
    public int TotalTickets { get; set; }
    public List<BookingAttendeePaymentDetailDto> Details { get; set; } = new();
}

public class BookingAttendeePaymentDetailDto
{
    public int AttendeeId { get; set; }
    public int ProductDocId { get; set; }
    public int MemberDocId { get; set; }
    public int NoOfBooking { get; set; }
    public int ProductType { get; set; }
    public string ProductTypeInString => ProductType switch
    {
        1 => "One-off",
        2 => "Trial",
        3 => "Payg",
        4 => "Monthly",
        _ => "Other"
    };
    public string? VenueName { get; set; }
    public decimal BookingAmount { get; set; }
    public int UserId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string MID { get; set; }
    public string? EmailAddress { get; set; }
    public string? ImageUrl { get; set; }
    public required string Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Address3 { get; set; }
    public string? PostCode { get; set; }
    public string? Town { get; set; }
    public string? County { get; set; }
    public string? Country { get; set; }
    public bool IsFormAvailable { get; set; }
    public int AttendeeDetailsStatus { get; set; }
    public string AttendeeDetailsStatusInString => AttendeeDetailsStatus switch
    {
        1 => "Active",
        2 => "Expire",
        3 => "Cancel",
        4 => "Transfer",
        _ => "Other"
    };
}