using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities.Enums
{
    public enum WorkFlowType
    {
        AssetReview = 1,
        AssetApprove = 2,
        Transfer = 3,
        Lease = 4,
        License = 5,
        Credential = 6,
        OwnerLeaseApproval = 7,
        OwnerTransferApproval = 8
    }
}
