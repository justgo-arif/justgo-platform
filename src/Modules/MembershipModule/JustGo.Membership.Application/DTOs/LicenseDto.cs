namespace JustGo.Membership.Application.DTOs
{
    public class LicenseDto
    {
        public int MemberDocId { get; set; }
        public int LicenseDocId { get; set; }
        public string LicenseId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? LicenceNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int ProductDocId { get; set; }
        public string LicenceType { get; set; } = string.Empty;
        public decimal ClubDocId { get; set; }
        public string CurrentStateId { get; set; } = string.Empty;
        public string? State { get; set; }
        public string? Color { get; set; }
        public decimal UnitPrice { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public string Benefits { get; set; } = string.Empty;
        public string LicenseCategory { get; set; } = string.Empty;
        public decimal RenewalWindow { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string ExpiryDateEndingUnit { get; set; } = string.Empty;
        public decimal ExpiryDateEndingValue { get; set; }

    }

}
