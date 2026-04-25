using CsvHelper.Configuration;

namespace JustGo.Finance.Application.DTOs.InstallmentDTOs
{
    public class InstallmentMap : ClassMap<InstallmentDto>
    {
        public InstallmentMap()
        {
            Map(x => x.CustomerName).Index(0).Name("Customers");
            Map(x => x.PlanName).Index(1).Name("Subscription Plan");
            Map(x => x.NextPaymentDate).Index(2).Name("Next Payment");
            Map(x => x.SchemeNo).Index(3).Name("Scheme No");
            Map(x => x.Amount).Index(4).Name("Amount");
            Map(x => x.Status).Index(5).Name("Status");
        }
    }
}
