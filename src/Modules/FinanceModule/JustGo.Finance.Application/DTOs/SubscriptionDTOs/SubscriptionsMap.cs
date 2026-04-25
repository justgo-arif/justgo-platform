using CsvHelper.Configuration;

namespace JustGo.Finance.Application.DTOs.SubscriptionDTOs
{
    public class SubscriptionsMap : ClassMap<SubscriptionsDto>
    {
        public SubscriptionsMap()
        {
            Map(x => x.CustomerName).Index(0).Name("Customers");
            Map(x => x.PlanName).Index(1).Name("Subscription Plan");
            Map(x => x.CreatedOn).Index(2).Name("Created On");
            Map(x => x.NextPaymentDate).Index(3).Name("Next Payment");
            Map(x => x.Amount).Index(4).Name("Amount");
            Map(x => x.Status).Index(5).Name("Status");
        }
    }
}
