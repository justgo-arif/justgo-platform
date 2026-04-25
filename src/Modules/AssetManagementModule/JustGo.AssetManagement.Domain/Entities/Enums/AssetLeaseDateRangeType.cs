using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities.Enums
{
    public enum AssetLeaseDateRangeType
    {
        Days = 1,
        Weeks = 2,
        Months = 3,
        [Description("Custom Date")]
        CustomRange = 4
    }
}
