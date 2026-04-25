using JustGo.AssetManagement.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Domain.Entities
{
    public class AssetLicense : RecordInfo
    {
        public int AssetLicenseId { get; set; }
        public int AssetId { get; set; }
        public int ProductId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int StatusId { get; set; }
        public int PaymentId { get; set; }
        public LicenseType LicenseType { get; set; }
        public LicenseCancelReason? CancelReason { get; set; }
        public DateTime? CancelEffectiveFrom { get; set; }
    }

}
