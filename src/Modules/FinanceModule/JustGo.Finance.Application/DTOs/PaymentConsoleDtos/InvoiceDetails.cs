namespace JustGo.Finance.Application.DTOs.PaymentConsoleDtos
{
    public class InvoiceDetails
    {
        public string? UserSyncId { get; set; } 
        public string? InvoiceTo { get; set; } = string.Empty;
        public string? Country { get; set; } = string.Empty;
        public int CountryId { get; set; }
        public string? Address1 { get; set; } = string.Empty;
        public string? Address2 { get; set; } = string.Empty;
        public string? Address3 { get; set; } = string.Empty;
        public string? Town { get; set; } = string.Empty;
        public string? County { get; set; } = string.Empty;
        public int CountyId { get; set; }
        public string? PostCode { get; set; } = string.Empty;
        public string PoNumber { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }
}
