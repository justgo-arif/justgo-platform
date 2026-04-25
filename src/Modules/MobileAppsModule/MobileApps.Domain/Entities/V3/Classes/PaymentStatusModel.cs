using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V3.Classes
{
    public class PaymentStatusModel
    {
        public DateTime? PaymentDate { get; set; }     // ap.PaymentDate (nullable)
        public DateTime StartDate { get; set; }        // oc.StartDate
        public int Status { get; set; }                // ps.[Status]
        public string PaymentStatus { get; set; }      // CASE ... AS PaymentStatus
        public int EntityDocId { get; set; }           // att.EntityDocId
        public int ProductId { get; set; }             // pl.ProductId
        public int SessionId { get; set; }             // att.SessionId
    }

}
