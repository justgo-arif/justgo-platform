namespace JustGo.Membership.Application.Features.Memberships.Queries.GetMemberMemberships
{
    public class MemberMemberships
    {
        public Guid UserSyncId { get; set; }
        public int UserId { get; set; }
        public int MemberDocId { get; set; }
        public int ProductId { get; set; }
        public int LicenceOwner { get; set; }
        public string MembershipPicUrl { get; set; } = string.Empty;
        public required string MembershipSyncGuid { get; set; }
        public string MembershipName { get; set; } = string.Empty;
        public string Licencetype { get; set; } = string.Empty;
        public string Expirydateendingunit { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal? UnitPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public int MemberLicenseDocId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int IsRenewalWindow { get; set; }
        public int IsUpgradeable { get; set; }
        public bool IsDownloadAvailable { get; set; }
        public string RenewalStatus { get; set; } = string.Empty;
        public string NextPaymentDate { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public bool Paymentfailed { get; set; }
        public bool Historical { get; set; }
        public Guid? PlanGuid { get; set; }
        public string? Progress { get; set; }
        public int CustomerId { get; set; }
    }
}