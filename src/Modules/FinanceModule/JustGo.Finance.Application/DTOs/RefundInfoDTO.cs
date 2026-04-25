namespace JustGo.Finance.Application.DTOs
{
    public class RefundInfoDto
    {
        public string ReferenceID { get; set; } = string.Empty;
        public string RefundedBy { get; set; } = string.Empty;
        public DateTime? IssueDate { get; set; }
        public decimal GrossAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
