using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities.Enums
{
    public enum ActionStatus
    {
        Approve = 1,
        Reject = 2,
        [Description("Send Back")]
        SendBack = 3
    }
}
