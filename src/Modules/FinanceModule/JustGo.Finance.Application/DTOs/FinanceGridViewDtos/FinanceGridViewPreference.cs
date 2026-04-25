namespace JustGo.Finance.Application.DTOs.FinanceGridViewDtos
{
    public class FinanceGridViewPreference
    {
        public int Id { get; set; }
        public int ViewId { get; set; }
        public int UserId { get; set; }
        public bool IsPinned { get; set; } = false;
        public bool IsDefault { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
