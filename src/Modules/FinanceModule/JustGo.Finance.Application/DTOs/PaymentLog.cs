namespace JustGo.Finance.Application.DTOs
{
    public class PaymentLog
    {
        public int RowId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Logtype { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Time { get; set; } = string.Empty;
    }
}
