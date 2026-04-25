using System.ComponentModel;

namespace JustGo.AssetManagement.Domain.Entities.Enums
{
    public enum LeaseStatusType
    {
        Active,
        [Description("Pending Payment")]
        PendingPayment,
        [Description("Pending Confirmation")]
        PendingConfirmation,
        Cancelled,
        [Description("Pending Approval")]
        PendingApproval,
        Rejected,
        Expired,
        [Description("Pending Owner Approval")]
        PendingOwnerApproval,
        Scheduled

    }
}
