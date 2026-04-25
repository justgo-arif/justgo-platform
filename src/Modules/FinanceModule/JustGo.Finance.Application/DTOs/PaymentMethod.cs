namespace JustGo.Finance.Application.DTOs
{
    public class PaymentMethod
    {
        public string Id { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string Expires { get; set; } = string.Empty; // Format: MM/YYYY
        public string CardType { get; set; } = string.Empty;
        public string Fingerprint { get; set; } = string.Empty;
        public string CardOwner { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string ZipCheck { get; set; } = string.Empty;
        public string SetupForFutureUse { get; set; } = string.Empty;
        public string LastFourDigits { get; set; } = string.Empty;
    }
}
