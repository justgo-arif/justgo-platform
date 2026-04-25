namespace JustGo.Booking.Application.DTOs.ProfileBookingDtos
{

    public class ProfileCourseBookingDto
    {
        public int DocId { get; set; }
        public int EventDocId { get; set; } 
        public Guid SyncGuid { get; set; }
        public string? EventName { get; set; }
        public string? CourseName { get; set; }
        public string? EventPicUrl { get; set; }
        public string? FormattedDateTime { get; set; }
        public DateTime? StartDateTime { get; set; }
        public string? AddressSummary { get; set; }
        public string? GoogleWalletURL { get; set; }
        public string? AppleWalletURL { get; set; }
        public string? ReceiptUrl { get; set; }
        public List<string>Attachments { get; set; } = new List<string>();
        public bool PastEvent { get; set; }
        public string? EventPeriod { get; set; }
        public string? ClubName { get; set; } 
        public string? ClubImageUrl { get; set; }  
        public string? ClubGuid { get; set; }
    }
}

