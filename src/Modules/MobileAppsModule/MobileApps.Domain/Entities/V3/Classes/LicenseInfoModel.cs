using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApps.Domain.Entities.V3.Classes
{
    public class LicenseInfoModel
    {
        public int OwnerId { get; set; }
        public int LicenseDocId { get; set; }
        public string ClassificationLicenseDocId { get; set; }
        public string ConditionType { get; set; }
        public bool IsConditionType { get; set; }
    }
 
}
