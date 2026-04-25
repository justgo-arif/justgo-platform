namespace JustGo.Finance.Application.DTOs.FinanceGridViewDtos
{
    public class FinanceGridViewDto
    {
        public int ViewId { get; set; }
        public string Name { get; set; } = null!;
        public dynamic Payload { get; set; } = null!; 
        public string MerchantId { get; set; } = null!;
        public bool IsSystemDefault { get; set; }
        public bool IsShared { get; set; }
        public bool IsPinned { get; set; } 
    }
}
