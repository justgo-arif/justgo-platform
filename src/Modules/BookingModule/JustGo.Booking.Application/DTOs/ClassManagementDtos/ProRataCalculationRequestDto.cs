namespace JustGo.Booking.Application.DTOs.ClassManagementDtos
{
    public class ProRataCalculationRequestDto
    {
        public required int ClassProductId { get; set; }
        public required DateTime StartDate { get; set; }
    }
}
