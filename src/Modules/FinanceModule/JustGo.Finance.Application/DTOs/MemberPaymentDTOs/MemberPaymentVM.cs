namespace JustGo.Finance.Application.DTOs.MemberPaymentDTOs
{
    public class MemberPaymentVm
    {
        public List<MemberPayment> Payments { get; set; }
        public bool HasNextPage { get; set; }
        public int? NextLastPaymentId { get; set; }
    }

}
