using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities.Enums
{
    public enum LicenseCancelReason
    {
        None = 0,
        [Description("Horse transfered to another owner")]
        HorseTransferedToAnotherOwner = 1,
        [Description("Horse transferred to another state")]
        HorseTransferredToAnotherState = 2,
        [Description("Horse no longer active")]
        HorseNoLongerActive = 3,
        [Description("Non-compliance with requirements")]
        NonComplianceWithRequirements = 4,
        [Description("Payment not received")]
        PaymentNotReceived = 5,
        [Description("Cancelled at owner’s request")]
        CancelledAtOwnersRequest = 6,
        [Description("Other")]
        Other = 7

    }
}
