namespace JustGo.Finance.Application.DTOs
{
    public class MerchantEntityIdentifiersDto
    {
        public int MerchantId { get; set; }
        public Guid MerchantGuid { get; set; }
        public int EntityId { get; set; }
        public Guid EntityGuid { get; set; }
    }
}
