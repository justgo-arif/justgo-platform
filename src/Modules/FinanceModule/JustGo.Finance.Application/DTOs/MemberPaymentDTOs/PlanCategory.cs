namespace JustGo.Finance.Application.DTOs.MemberPaymentDTOs
{
    public class PlanCategory
    {

        public string Name { get; set; } = string.Empty;

        public List<Plan> Plans { get; set; } = new();
    }

}
