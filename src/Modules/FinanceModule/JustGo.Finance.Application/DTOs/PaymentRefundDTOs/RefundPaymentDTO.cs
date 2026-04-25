using JustGo.Finance.Application.DTOs.Enums;

namespace JustGo.Finance.Application.DTOs.PaymentRefundDTOs
{
    public class RefundPaymentDto
    {
        public RequestSource Source { get; set; }  
        public Guid? MemberId { get; set; }       
        public Guid? MerchantId { get; set; }      
        public Guid PaymentId { get; set; }
        public RefundType RequestRefundType { get; set; }
        public int RefundReasonId { get; set; }
        public string RefundNote { get; set; } = string.Empty;
        public bool IsSendNotification { get; set; }
        public List<RefundableItemCommand>? RefundItems { get; set; }
    }
}
