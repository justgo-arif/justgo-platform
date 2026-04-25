namespace JustGo.Finance.Application.DTOs
{
    public class Product
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Surcharge { get; set; }
        public decimal Discount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? Proratadiscount { get; set; }
        public decimal? Transactionfee { get; set; }
        public string ProductImageURL { get; set; } = string.Empty;
        public bool Exclusivetransactionfeescalcultion { get; set; } 
        public List<PurchaseMember>? members { get; set; }

    }
}
