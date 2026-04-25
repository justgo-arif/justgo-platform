using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Finance.Application.DTOs.Balances
{
    public class AdyenPayoutInfoDTO
    {
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public string? TransferId { get; set; }
        public string? Status { get; set; }
        public string? ExternalAccount { get; set; }
        public string? Descriptions { get; set; }
        public string? StatementDescriptor { get; set; }
        public DateTime Initiated { get; set; }
        public DateTime EstimatedArrival { get; set; }
        public string UserFriendlyStatus
        {
            get
            {
                if (string.IsNullOrEmpty(Status))
                    return Status;

                var lowerStatus = Status.ToLower();
                if (lowerStatus == "received")
                    return "Payout Initiated";
                if (lowerStatus == "authorized")
                    return "Awaiting Processing";
                if (lowerStatus == "booked")
                    return "Processing Payout";
                if (lowerStatus == "credited")
                    return "Payout Completed";
                if (lowerStatus == "returned")
                    return "Payout Failed";

                return Status;
            }
        }
    }
}
