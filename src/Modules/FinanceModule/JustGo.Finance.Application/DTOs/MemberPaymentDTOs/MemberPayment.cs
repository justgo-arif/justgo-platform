namespace JustGo.Finance.Application.DTOs.MemberPaymentDTOs
{
    public class MemberPayment
    {
        public string  Id { get; set; }  
        public int  DocId { get; set; } 
        public string PaymentId { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string? PaymentDateTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty; 
        public decimal GrossAmount { get; set; }
        public int TotalItems { get; set; }
        public List<MemberPaymentMerchantVm> Merchants { get; set; } = new List<MemberPaymentMerchantVm>();
        public List<MemberPaymentInfoDto>? Items { get; set; } = new List<MemberPaymentInfoDto>();
        
    }
}
