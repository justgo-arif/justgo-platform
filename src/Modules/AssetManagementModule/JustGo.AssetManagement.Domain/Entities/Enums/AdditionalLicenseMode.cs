using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities.Enums
{
    public enum AdditionalLicenseMode
    {
        [Description("Visible And Required")]
        VisibleAndRequired = 1,
        [Description("Visible Not Required")]
        VisibleNotRequired = 2,
        [Description("Not Visible")]
        NotVisible = 3
    }
}
