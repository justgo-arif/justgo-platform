namespace JustGo.Membership.Domain.Entities
{
    public class MembershipDownloadLinks
    { 
        public string PdfUrl { get; set; } = string.Empty;
        public string GoogleWalletUrl { get; set; } = string.Empty;
        public string AppleWalletUrl { get; set; } = string.Empty;
        public string? qrCodeUrl { get; set; } = string.Empty;
    }
}
