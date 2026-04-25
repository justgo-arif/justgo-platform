using System.ComponentModel;

namespace JustGo.AssetManagement.Domain.Entities.Enums
{
    public enum LicenseStatusType
    {
        Active,
        Expired,
        [Description("Awaiting Approval")]
        AwaitingApproval,
        Suspended,
        [Description("Pending Payment")]
        PendingPayment
    }
}
