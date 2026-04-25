using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities.Enums
{
    public enum CredentialStatusType
    {
        Active,
        [Description("Credentials Expired")]
        CredentialsExpired,
        Expired,
        [Description("Awaiting Approval")]
        AwaitingApproval,
        Suspended,
        PendingPayment,
        Cancelled
    }
}
