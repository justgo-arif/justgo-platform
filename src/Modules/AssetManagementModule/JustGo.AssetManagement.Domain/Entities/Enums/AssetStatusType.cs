using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities.Enums
{
    public enum AssetStatusType
    {
        Draft,
        [Description("Pending Action")]
        PendingAction,
        [Description("Under Review")]
        UnderReview,
        [Description("Pending Approval")]
        PendingApproval,
        Active,
        Suspended,
        Inactive,
        Archived

    }
}
