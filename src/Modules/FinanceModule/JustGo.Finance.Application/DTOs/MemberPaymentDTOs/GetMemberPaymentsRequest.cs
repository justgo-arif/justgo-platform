using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Finance.Application.DTOs.MemberPaymentDTOs
{
    public class GetMemberPaymentsRequest
    { 
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<string>? PaymentMethods { get; set; }
        public List<int>? StatusIds { get; set; }
        public string? SearchText { get; set; }
        public string? ScopeKey { get; set; }
        public int PageSize { get; set; }
        public int? LastPaymentId { get; set; }
    }
}
