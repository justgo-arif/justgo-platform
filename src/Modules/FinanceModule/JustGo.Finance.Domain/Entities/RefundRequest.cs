using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Finance.Domain.Entities
{
    public class RefundRequest
    {
        public int Id { get; set; }
        public int PaymentId { get; set; }
        public string? RefundType { get; set; }
        public int RefundReasonId { get; set; }
        public string? RefundNote { get; set; }
        public bool IsSendNotification { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
