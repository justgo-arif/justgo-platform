using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Finance.Domain.Entities
{
    public class PaymentRefundQueue
    {
        public int PaymentRefundQueueId { get; set; }
        public string? RefundArea { get; set; }
        public long PaymentDocId { get; set; }
        public int PaymentId { get; set; }
        public long? ProductDocId { get; set; }
        public long? TypeEntityDocId { get; set; }
        public long? ForEntityDocId { get; set; }
        public DateTime CreationDate { get; set; }
        public bool Executed { get; set; }
        public DateTime? ExecutionDate { get; set; }
        public string? ExecutionDetails { get; set; }
        public string? RequestRefundType { get; set; }
        public decimal RequestRefundAmmount { get; set; }
        public decimal ApplicableRefundAmmount { get; set; }
        public bool RequestRefundWithPaymentFees { get; set; }
        public int ActionUserId { get; set; }
    }

}
