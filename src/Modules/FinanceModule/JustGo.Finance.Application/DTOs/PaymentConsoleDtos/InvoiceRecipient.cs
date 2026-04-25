namespace JustGo.Finance.Application.DTOs.PaymentConsoleDtos
{
    public class InvoiceRecipient
    {
        public int UserId { get; set; }
        public string? InvoiceTo { get; set; }
        public string? EmailAddress { get; set; }
        public string? Country { get; set; }
        public int CountryId { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Address3 { get; set; }
        public string? Town { get; set; }
        public string? County { get; set; }
        public int CountyId { get; set; }
        public string? PostCode { get; set; }
    }

}
