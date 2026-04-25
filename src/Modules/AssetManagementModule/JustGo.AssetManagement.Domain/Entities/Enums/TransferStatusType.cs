using System.ComponentModel;

namespace JustGo.AssetManagement.Domain.Entities.Enums
{
    public enum TransferStatusType
    {
        Completed,
        [Description("Pending Payment")]
        PendingPayment,
        [Description("Pending Confirmation")]
        PendingConfirmation,
        [Description("Pending Approval")]
        PendingApproval,
        Rejected,
        [Description("Pending Owner Approval")]
        PendingOwnerApproval

    }

   
}
