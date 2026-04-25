using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.DTOs
{

    public class AssetLicenseDTO
    {
        public string AssetLicenseId { get; set; }
        public string LicenseStatus { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? CancelEffectiveFrom { get; set; }
        public string Name { get; set; }
        public string OwnerId { get; set; }
        public string LicenseId { get; set; }
        public string ProductId { get; set; }
        public bool IsUpgradable { get; set; }

    }

}
