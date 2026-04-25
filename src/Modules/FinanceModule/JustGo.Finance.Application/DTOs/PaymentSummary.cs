namespace JustGo.Finance.Application.DTOs
{
    public class PaymentSummary
    {
        public decimal? TotalAmount { get; set; }
        public decimal? SubTotal { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? TaxRate { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? Surcharge { get; set; } 
        public decimal? Proratadiscount { get; set; } 
        public decimal? Transactionfee { get; set; } 
        public string? DiscountCode { get; set; }
        public string? Currency { get; set; }
        public int ItemsCount { get; set; }
        public string? DownloadPath { get; set; }
        public bool Exclusivetransactionfeescalcultion { get; set; }

    }
}
