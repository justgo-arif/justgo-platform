using CsvHelper.Configuration;
using JustGo.Finance.Application.DTOs.ExportDTOs;

namespace JustGo.Finance.Application.DTOs
{
    public class PaymentInfoMap : ClassMap<ExportedReceiptDto>
    {
        public PaymentInfoMap()
        {
            Map(x => x.PaymentId).Index(0).Name("Payment ID");
            Map(x => x.CustomerName).Index(1).Name("Customers");
            Map(x => x.TotalAmount).Index(2).Name("Amount");
            Map(x => x.PaymentMethod).Index(3).Name("Payment Method");
            Map(x => x.PaymentType).Index(4).Name("Payment Type");
            Map(x => x.ReceiptStatus).Index(5).Name("Status");
            Map(x => x.PaymentDate).Index(6).Name("Payment Date");
            Map(x => x.Products).Index(7).Name("Products");
        }
    }
}
