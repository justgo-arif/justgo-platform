namespace JustGo.Membership.Application.DTOs
{
    public class MemberLicenseDto
    {
        public string Location { get; set; }=string.Empty;
        public int LicenseDocId { get; set; }
        public int ProductDocId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Benefits { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string HidePrice { get; set; } = string.Empty;
        public int Sequence { get; set; }
        public int LicenceOwner { get; set; }
        public string Code { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal Availablequantity { get; set; }
        public string ProductColor { get; set; } = string.Empty;
        public decimal? RenewalWindow { get; set; }
        public int? InactiveWindow { get; set; }
        public string LicenseType { get; set; } = string.Empty;
        public string ExpiryDateEndingUnit { get; set; } = string.Empty;
        public int? ExpiryDateEndingValue { get; set; }
        public bool HideMembershipDuration { get; set; }
        public bool HideViewMoreAboutMembership { get; set; }
        public string Classification { get; set; } = string.Empty;
        public decimal? PriceOption { get; set; }
        public decimal? FromPrice { get; set; }
        public decimal? ToPrice { get; set; }
        public string MembershipJourney { get; set; } = string.Empty;
        public string AlternateDisplayCurrency { get; set; } = string.Empty;
        public bool IsSubscriptionEnabled { get; set; }
        public string Recurringdescription { get; set; } = string.Empty;
        public bool RecurringMandatory { get; set; }
        public int UpgradeType { get; set; }
        public int UpgradeId { get; set; }
        public string ExpiryDateStartingType { get; set; } = string.Empty;
        public string ExpiryDateStartingValue { get; set; }= string.Empty;
        public int InstallmentInitialPayDocId { get; set; }
        public string LicenseConfig { get; set; } = string.Empty;

    }

}
